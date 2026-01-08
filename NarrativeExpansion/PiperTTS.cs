using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BepInEx.Logging;
using UnityEngine;

namespace NarrativeExpansion
{
    /// <summary>
    /// Cross-platform Piper TTS integration for NPC voices.
    /// Works on Windows and Linux.
    /// </summary>
    public class PiperTTS : IDisposable
    {
        private static PiperTTS _instance;
        public static PiperTTS Instance => _instance;

        private readonly string _piperPath;
        private readonly string _modelPath;
        private readonly string _cacheDir;
        private readonly ManualLogSource _logger;
        private readonly bool _isAvailable;

        // Thread-safe queues
        private readonly ConcurrentQueue<TTSRequest> _pendingRequests = new ConcurrentQueue<TTSRequest>();
        private readonly ConcurrentQueue<TTSResult> _completedResults = new ConcurrentQueue<TTSResult>();
        private readonly ConcurrentDictionary<string, AudioClip> _audioCache = new ConcurrentDictionary<string, AudioClip>();

        private Thread _workerThread;
        private volatile bool _running = true;
        private int _requestId = 0;

        public bool IsAvailable => _isAvailable;

        public static void Initialize(string pluginPath, ManualLogSource logger)
        {
            if (_instance != null) return;
            _instance = new PiperTTS(pluginPath, logger);
        }

        private PiperTTS(string pluginPath, ManualLogSource logger)
        {
            _logger = logger;

            // Determine platform-specific executable
            string piperDir = Path.Combine(pluginPath, "piper");
            string exeName = Application.platform == RuntimePlatform.WindowsPlayer ||
                            Application.platform == RuntimePlatform.WindowsEditor
                ? "piper.exe"
                : "piper";

            _piperPath = Path.Combine(piperDir, exeName);
            _modelPath = FindVoiceModel(piperDir);
            _cacheDir = Path.Combine(Path.GetTempPath(), "NarrativeExpansion_TTS");

            // Check if Piper is available
            if (!File.Exists(_piperPath))
            {
                _logger?.LogWarning($"Piper TTS not found at: {_piperPath}");
                _logger?.LogInfo("NPCs will use fallback procedural voice");
                _isAvailable = false;
                return;
            }

            if (string.IsNullOrEmpty(_modelPath))
            {
                _logger?.LogWarning("No Piper voice model found");
                _isAvailable = false;
                return;
            }

            // Create cache directory
            try
            {
                Directory.CreateDirectory(_cacheDir);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create cache dir: {ex.Message}");
                _isAvailable = false;
                return;
            }

            // Make executable on Linux
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor)
            {
                try
                {
                    var chmod = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{_piperPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(chmod)?.WaitForExit(5000);
                }
                catch { }
            }

            _isAvailable = true;
            _logger?.LogInfo($"Piper TTS initialized: {Path.GetFileName(_modelPath)}");

            // Start worker thread
            _workerThread = new Thread(WorkerLoop) { IsBackground = true, Name = "PiperTTS" };
            _workerThread.Start();
        }

        private string FindVoiceModel(string piperDir)
        {
            if (!Directory.Exists(piperDir)) return null;

            // Look for .onnx voice model files
            var onnxFiles = Directory.GetFiles(piperDir, "*.onnx", SearchOption.AllDirectories);
            if (onnxFiles.Length > 0)
            {
                // Prefer medium quality if available
                foreach (var file in onnxFiles)
                {
                    if (file.Contains("medium")) return file;
                }
                return onnxFiles[0];
            }

            return null;
        }

        /// <summary>
        /// Request TTS generation (non-blocking)
        /// </summary>
        public void Speak(string text, Action<AudioClip> onComplete, float speed = 1.0f)
        {
            if (!_isAvailable || string.IsNullOrWhiteSpace(text))
            {
                onComplete?.Invoke(null);
                return;
            }

            // Check cache
            string cacheKey = GetCacheKey(text, speed);
            if (_audioCache.TryGetValue(cacheKey, out var cached))
            {
                onComplete?.Invoke(cached);
                return;
            }

            // Queue request
            _pendingRequests.Enqueue(new TTSRequest
            {
                Id = Interlocked.Increment(ref _requestId),
                Text = text,
                Speed = speed,
                CacheKey = cacheKey,
                Callback = onComplete
            });
        }

        /// <summary>
        /// Call from Update() to process completed audio on main thread
        /// </summary>
        public void ProcessResults()
        {
            while (_completedResults.TryDequeue(out var result))
            {
                AudioClip clip = null;

                if (result.Success && File.Exists(result.WavPath))
                {
                    try
                    {
                        clip = WavLoader.LoadWav(result.WavPath, $"TTS_{result.Request.Id}");
                        if (clip != null)
                        {
                            _audioCache[result.Request.CacheKey] = clip;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Failed to load WAV: {ex.Message}");
                    }

                    // Clean up temp file
                    try { File.Delete(result.WavPath); } catch { }
                }

                result.Request.Callback?.Invoke(clip);
            }
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                if (_pendingRequests.TryDequeue(out var request))
                {
                    ProcessRequest(request);
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }

        private void ProcessRequest(TTSRequest request)
        {
            string outputPath = Path.Combine(_cacheDir, $"tts_{request.Id}.wav");
            bool success = false;

            try
            {
                // Calculate length scale (inverse - lower = faster)
                float lengthScale = 1.0f / Mathf.Clamp(request.Speed, 0.5f, 2.0f);

                var startInfo = new ProcessStartInfo
                {
                    FileName = _piperPath,
                    Arguments = $"-m \"{_modelPath}\" --length-scale {lengthScale:F2} --output_file \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Write text to stdin
                    process.StandardInput.WriteLine(request.Text);
                    process.StandardInput.Close();

                    // Wait with timeout (30 seconds)
                    if (process.WaitForExit(30000))
                    {
                        success = process.ExitCode == 0 && File.Exists(outputPath);
                    }
                    else
                    {
                        try { process.Kill(); } catch { }
                        _logger?.LogWarning($"TTS timed out for: {request.Text.Substring(0, Math.Min(30, request.Text.Length))}...");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"TTS error: {ex.Message}");
            }

            _completedResults.Enqueue(new TTSResult
            {
                Request = request,
                WavPath = outputPath,
                Success = success
            });
        }

        private string GetCacheKey(string text, float speed)
        {
            return $"{text}_{speed:F1}".GetHashCode().ToString("X8");
        }

        public void Dispose()
        {
            _running = false;
            _workerThread?.Join(2000);

            // Clean up cache directory
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    foreach (var file in Directory.GetFiles(_cacheDir, "tts_*.wav"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }

            _instance = null;
        }

        private struct TTSRequest
        {
            public int Id;
            public string Text;
            public float Speed;
            public string CacheKey;
            public Action<AudioClip> Callback;
        }

        private struct TTSResult
        {
            public TTSRequest Request;
            public string WavPath;
            public bool Success;
        }
    }

    /// <summary>
    /// Utility to load WAV files into Unity AudioClip
    /// </summary>
    public static class WavLoader
    {
        public static AudioClip LoadWav(string filePath, string clipName = "wav")
        {
            if (!File.Exists(filePath)) return null;

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return LoadWav(fileBytes, clipName);
        }

        public static AudioClip LoadWav(byte[] wavData, string clipName = "wav")
        {
            if (wavData == null || wavData.Length < 44) return null;

            try
            {
                // Validate RIFF header
                if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
                    return null;

                // Parse format chunk
                int channels = BitConverter.ToInt16(wavData, 22);
                int sampleRate = BitConverter.ToInt32(wavData, 24);
                int bitDepth = BitConverter.ToInt16(wavData, 34);

                // Find data chunk
                int dataOffset = 12;
                int dataSize = 0;

                while (dataOffset < wavData.Length - 8)
                {
                    string chunkId = System.Text.Encoding.ASCII.GetString(wavData, dataOffset, 4);
                    int chunkSize = BitConverter.ToInt32(wavData, dataOffset + 4);

                    if (chunkId == "data")
                    {
                        dataOffset += 8;
                        dataSize = chunkSize;
                        break;
                    }

                    dataOffset += 8 + chunkSize;
                }

                if (dataSize == 0) return null;

                // Convert to float samples
                float[] samples = ConvertToFloatSamples(wavData, dataOffset, dataSize, bitDepth);
                if (samples == null) return null;

                // Create AudioClip
                int sampleCount = samples.Length / channels;
                AudioClip clip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
                clip.SetData(samples, 0);

                return clip;
            }
            catch
            {
                return null;
            }
        }

        private static float[] ConvertToFloatSamples(byte[] data, int offset, int size, int bitDepth)
        {
            int bytesPerSample = bitDepth / 8;
            int sampleCount = size / bytesPerSample;
            float[] samples = new float[sampleCount];

            switch (bitDepth)
            {
                case 16: // Most common - Piper outputs 16-bit
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = BitConverter.ToInt16(data, offset + i * 2);
                        samples[i] = sample / 32768f;
                    }
                    break;

                case 8:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        samples[i] = (data[offset + i] - 128) / 128f;
                    }
                    break;

                case 24:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int sample = data[offset + i * 3] |
                                    (data[offset + i * 3 + 1] << 8) |
                                    (data[offset + i * 3 + 2] << 16);
                        if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000);
                        samples[i] = sample / 8388608f;
                    }
                    break;

                case 32:
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int sample = BitConverter.ToInt32(data, offset + i * 4);
                        samples[i] = sample / 2147483648f;
                    }
                    break;

                default:
                    return null;
            }

            return samples;
        }
    }
}
