using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Program
{
    static ModuleDefMD module = null!;

    static void Main(string[] args)
    {
        string dllPath = args.Length > 0 ? args[0] :
            @"C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default\BepInEx\plugins\Equinox-EMUBuilder\EMUBuilder\EMUBuilder.dll";

        PatchEMUBuilder(dllPath);
    }

    static void PatchEMUBuilder(string dllPath)
    {
        Console.WriteLine("=== Patching EMUBuilder for WaterWheel and PowerGenerator support ===");

        byte[] dllBytes = File.ReadAllBytes(dllPath);
        module = ModuleDefMD.Load(dllBytes);

        PatchBuildMachineSwitch();
        PatchSupportedMachineTypes();

        using (var ms = new MemoryStream())
        {
            module.Write(ms);
            File.WriteAllBytes(dllPath, ms.ToArray());
        }

        Console.WriteLine("Saved patched EMUBuilder.dll");
    }

    static void PatchBuildMachineSwitch()
    {
        var machineBuilder = module.GetTypes().FirstOrDefault(t => t.Name == "MachineBuilder");
        if (machineBuilder == null)
        {
            Console.WriteLine("ERROR: Could not find MachineBuilder class");
            return;
        }

        var buildMachine = machineBuilder.Methods.FirstOrDefault(m => m.Name == "BuildMachine");
        if (buildMachine == null || buildMachine.Body == null)
        {
            Console.WriteLine("ERROR: Could not find BuildMachine method");
            return;
        }

        Console.WriteLine("Found BuildMachine method");

        var instructions = buildMachine.Body.Instructions;
        Instruction switchInstr = null;

        for (int i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCodes.Switch)
            {
                switchInstr = instructions[i];
                break;
            }
        }

        if (switchInstr == null)
        {
            Console.WriteLine("ERROR: Could not find switch instruction");
            return;
        }

        var targets = (Instruction[])switchInstr.Operand;
        Console.WriteLine("Found switch with " + targets.Length + " cases");

        if (targets.Length < 35)
        {
            var simpleBuildTarget = targets[2];
            var newTargets = new Instruction[35];
            for (int i = 0; i < 35; i++)
            {
                if (i < targets.Length)
                    newTargets[i] = targets[i];
                else
                    newTargets[i] = simpleBuildTarget;
            }
            switchInstr.Operand = newTargets;
            Console.WriteLine("Extended switch table to 35 cases");
        }

        var currentTargets = (Instruction[])switchInstr.Operand;
        var simpleBuild = currentTargets[2];

        // Cases to add to DoSimpleBuild (case = enum - 1):
        // 2=Chest(3), 5=Elevator(6), 8=LightningRod(9), 9=Planter(10), 10=PowerGen(11),
        // 13=SolarPanel(14), 14=SonarScanner(15), 22=WaterWheel(23), 24=Accumulator(25),
        // 29=MemoryTree(30) for AtlantumReactor
        int[] casesToAdd = { 2, 5, 8, 9, 10, 13, 14, 22, 24, 29 };
        foreach (int caseNum in casesToAdd)
        {
            if (caseNum < currentTargets.Length)
            {
                currentTargets[caseNum] = simpleBuild;
                Console.WriteLine("Case " + caseNum + ": redirected to DoSimpleBuild");
            }
        }
    }

    static void PatchSupportedMachineTypes()
    {
        var emuBuilder = module.GetTypes().FirstOrDefault(t => t.Name == "EMUBuilder");
        if (emuBuilder == null)
        {
            Console.WriteLine("ERROR: Could not find EMUBuilder class");
            return;
        }

        var supportedField = emuBuilder.Fields.FirstOrDefault(f => f.Name == "SupportedMachineTypes");
        if (supportedField == null)
        {
            Console.WriteLine("ERROR: Could not find SupportedMachineTypes field");
            return;
        }

        Console.WriteLine("Found SupportedMachineTypes field: " + supportedField.FieldType);

        var cctor = emuBuilder.Methods.FirstOrDefault(m => m.Name == ".cctor");
        if (cctor == null || cctor.Body == null)
        {
            Console.WriteLine("ERROR: Could not find static constructor");
            return;
        }

        Console.WriteLine("Found static constructor");

        // Keep old max stack to avoid calculation issues
        cctor.Body.KeepOldMaxStack = true;

        var instructions = cctor.Body.Instructions;

        // Find stsfld SupportedMachineTypes
        int stsfldIndex = -1;
        for (int i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCodes.Stsfld &&
                instructions[i].Operand is FieldDef fd &&
                fd.Name == "SupportedMachineTypes")
            {
                stsfldIndex = i;
                break;
            }
        }

        if (stsfldIndex == -1)
        {
            Console.WriteLine("ERROR: Could not find stsfld SupportedMachineTypes");
            return;
        }

        Console.WriteLine("Found stsfld at index " + stsfldIndex);

        // DON'T change newarr size - RuntimeHelpers.InitializeArray needs exact match
        // Instead, after stsfld, resize the array and add WaterWheel

        // Get the element type for newarr
        var elementType = ((SZArraySig)supportedField.FieldType).Next;

        // Add local variables
        var newArrLocal = new Local(supportedField.FieldType);
        var lenLocal = new Local(module.CorLibTypes.Int32);
        cctor.Body.Variables.Add(newArrLocal);
        cctor.Body.Variables.Add(lenLocal);

        // Get Array.Copy method reference
        var mscorlibRef = module.CorLibTypes.Object.TypeRef.DefinitionAssembly;
        var arrayTypeRef = new TypeRefUser(module, "System", "Array", mscorlibRef.ToAssemblyRef());
        var arrayCopySig = MethodSig.CreateStatic(module.CorLibTypes.Void,
            new ClassSig(arrayTypeRef),
            new ClassSig(arrayTypeRef),
            module.CorLibTypes.Int32);
        var arrayCopyRef = new MemberRefUser(module, "Copy", arrayCopySig, arrayTypeRef);

        // Machine types to add to SupportedMachineTypes for copy/paste support:
        // 3=Chest (WormholeChests), 10=Planter (MorePlanters MKII/III),
        // 11=PowerGenerator, 23=WaterWheel, 24=Accumulator,
        // 25=HighVoltageCable, 26=VoltageStepper, 30=MemoryTree (AtlantumReactor)
        int[] machineTypesToAdd = { 3, 10, 11, 23, 24, 25, 26, 30 };
        int numToAdd = machineTypesToAdd.Length;

        var insertPoint = stsfldIndex + 1;
        var instrList = new System.Collections.Generic.List<Instruction>();

        // Load old array and get its length
        instrList.Add(new Instruction(OpCodes.Ldsfld, supportedField));
        instrList.Add(new Instruction(OpCodes.Dup));
        instrList.Add(new Instruction(OpCodes.Ldlen));
        instrList.Add(new Instruction(OpCodes.Conv_I4));
        instrList.Add(new Instruction(OpCodes.Stloc, lenLocal));

        // Create new array of size len + numToAdd
        instrList.Add(new Instruction(OpCodes.Ldloc, lenLocal));
        instrList.Add(Instruction.CreateLdcI4(numToAdd));
        instrList.Add(new Instruction(OpCodes.Add));
        instrList.Add(new Instruction(OpCodes.Newarr, elementType.ToTypeDefOrRef()));
        instrList.Add(new Instruction(OpCodes.Stloc, newArrLocal));

        // Array.Copy(oldArr, newArr, oldLen)
        instrList.Add(new Instruction(OpCodes.Ldloc, newArrLocal));
        instrList.Add(new Instruction(OpCodes.Ldloc, lenLocal));
        instrList.Add(new Instruction(OpCodes.Call, arrayCopyRef));

        // Add each new machine type
        for (int i = 0; i < machineTypesToAdd.Length; i++)
        {
            instrList.Add(new Instruction(OpCodes.Ldloc, newArrLocal));
            instrList.Add(new Instruction(OpCodes.Ldloc, lenLocal));
            if (i > 0)
            {
                instrList.Add(Instruction.CreateLdcI4(i));
                instrList.Add(new Instruction(OpCodes.Add));
            }
            instrList.Add(Instruction.CreateLdcI4(machineTypesToAdd[i]));
            instrList.Add(new Instruction(OpCodes.Stelem, elementType.ToTypeDefOrRef()));
            Console.WriteLine($"  Adding MachineType {machineTypesToAdd[i]} at index len+{i}");
        }

        // Store new array back to field
        instrList.Add(new Instruction(OpCodes.Ldloc, newArrLocal));
        instrList.Add(new Instruction(OpCodes.Stsfld, supportedField));

        for (int i = 0; i < instrList.Count; i++)
        {
            instructions.Insert(insertPoint + i, instrList[i]);
        }

        Console.WriteLine($"Added {numToAdd} machine types to SupportedMachineTypes");
        Console.WriteLine("Patched SupportedMachineTypes array");
    }
}
