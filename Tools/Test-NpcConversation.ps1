param([switch]$Live, [switch]$Offline)
$ErrorActionPreference = 'Stop'
$npcRoot = Split-Path $PSScriptRoot -Parent
$npcTemp = Join-Path $npcRoot 'Temp/NpcConversationChecks'
New-Item -ItemType Directory -Path $npcTemp -Force | Out-Null
# Изолирана проверка на реалните C# sources. Shim-ът не доказва Unity serialization или UI.
$npcProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="../../Assets/_Game/Scripts/Conversation/*.cs" Exclude="../../Assets/_Game/Scripts/Conversation/NpcChatUI.cs" />
    <Compile Include="../../Assets/_Game/Editor/NpcConversationChecks.cs" />
    <Compile Include="Harness.cs" />
  </ItemGroup>
</Project>
'@
Set-Content (Join-Path $npcTemp 'Checks.csproj') $npcProject
$npcHarness = @'
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
namespace UnityEngine {
    public class ScriptableObject {}
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class TextAreaAttribute : Attribute {}
    public static class Debug { public static void Log(object value) => Console.WriteLine(value); }
    public static class JsonUtility {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value) => JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);
    }
}
public static class Program {
    public static async Task Main(string[] args) {
        // Само flat fields от конкретния тестов YAML asset; не е общ Unity YAML reader.
        var profile = new NpcProfile();
        var knowledge = new List<NpcKnowledge>();
        NpcKnowledge current = null;
        foreach (var raw in File.ReadLines(args[0])) {
            var line = raw.Trim();
            if (line.StartsWith("- id: ")) { current = new NpcKnowledge { id = line.Substring(6) }; knowledge.Add(current); continue; }
            var colon = line.IndexOf(':'); if (colon < 0) continue;
            var key = line.Substring(0, colon); var value = line.Substring(colon + 1).Trim();
            if (current != null) {
                if (key == "category") current.category = value;
                if (key == "text") current.text = value;
                if (key == "requiredMaterialId") current.requiredMaterialId = value;
            } else {
                if (key == "personId") profile.personId = value;
                if (key == "displayName") profile.displayName = value;
                if (key == "role") profile.role = value;
                if (key == "speakingStyle") profile.speakingStyle = value;
            }
        }
        profile.knowledge = knowledge.ToArray();
        await NpcConversationChecks.Boundaries(profile);
        if (args.Contains("live")) await NpcConversationChecks.Live(profile);
        if (args.Contains("offline")) await NpcConversationChecks.Offline(profile);
    }
}
'@
Set-Content (Join-Path $npcTemp 'Harness.cs') $npcHarness
$npcArgs = @((Join-Path $npcRoot 'Assets/_Game/Resources/NpcPrototype/TestNpc.asset'))
if ($Live) { $npcArgs += 'live' }
if ($Offline) { $npcArgs += 'offline' }
dotnet run --project (Join-Path $npcTemp 'Checks.csproj') -- @npcArgs
if ($LASTEXITCODE -ne 0) { throw "NPC checks failed: $LASTEXITCODE" }
