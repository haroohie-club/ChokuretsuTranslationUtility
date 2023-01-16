using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.NDS.Overlay
{
    public class OverlayAsmHack
    {
        public static bool Insert(string path, Overlay overlay, string romInfoPath)
        {
            if (!Compile(path, overlay))
            {
                return false;
            }

            // Add a new symbols file based on what we just compiled so the replacements can reference the old symbols
            string[] newSym = File.ReadAllLines(Path.Combine(path, overlay.Name, "newcode.sym"));
            List<string> newSymbolsFile = new();
            foreach (string line in newSym)
            {
                Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w\s+.text\s+\d{8} (?<name>.+)");
                if (match.Success)
                {
                    newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
                }
            }
            File.WriteAllLines(Path.Combine(path, overlay.Name, "newcode.x"), newSymbolsFile);

            List<string> replFiles = new();
            foreach (string subdir in Directory.GetDirectories(Path.Combine(path, overlay.Name, "replSource")))
            {
                replFiles.Add($"repl_{Path.GetFileNameWithoutExtension(subdir)}");
                if (!CompileReplace(Path.GetRelativePath(path, subdir), path, overlay))
                {
                    return false;
                }
            }
            if (!File.Exists(Path.Combine(path, overlay.Name, "newcode.bin")))
            {
                return false;
            }
            foreach (string replFile in replFiles)
            {
                if (!File.Exists(Path.Combine(path, overlay.Name, $"{replFile}.bin")))
                {
                    return false;
                }
            }
            byte[] newCode = File.ReadAllBytes(Path.Combine(path, overlay.Name, "newcode.bin"));

            foreach (string line in newSym)
            {
                Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w\s+.text\s+\d{8} (?<name>.+)");
                if (match.Success)
                {
                    string[] nameSplit = match.Groups["name"].Value.Split('_');
                    switch (nameSplit[0])
                    {
                        case "ahook":
                            uint replaceAddress = uint.Parse(nameSplit[1], NumberStyles.HexNumber);
                            uint replace = 0xEB000000; //BL Instruction
                            uint destinationAddress = uint.Parse(match.Groups["address"].Value, NumberStyles.HexNumber);
                            uint relativeDestinationOffset = (destinationAddress / 4) - (replaceAddress / 4) - 2;
                            relativeDestinationOffset &= 0x00FFFFFF;
                            replace |= relativeDestinationOffset;
                            overlay.Patch(replaceAddress, BitConverter.GetBytes(replace));
                            break;
                    }
                }
            }

            foreach (string replFile in replFiles)
            {
                byte[] replCode = File.ReadAllBytes(Path.Combine(path, overlay.Name, $"{replFile}.bin"));
                uint replaceAddress = uint.Parse(replFile.Split('_')[1], NumberStyles.HexNumber);
                overlay.Patch(replaceAddress, replCode);
            }

            overlay.Append(newCode, romInfoPath);

            File.Delete(Path.Combine(path, overlay.Name, "newcode.bin"));
            File.Delete(Path.Combine(path, overlay.Name, "newcode.elf"));
            File.Delete(Path.Combine(path, overlay.Name, "newcode.sym"));
            File.Delete(Path.Combine(path, overlay.Name, "newcode.x"));
            foreach (string replFile in replFiles)
            {
                File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.bin"));
                File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.elf"));
                File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.sym"));
            }
            Directory.Delete(Path.Combine(path, "build"), true);
            return true;
        }

        private static bool Compile(string path, Overlay overlay)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "make",
                Arguments = $"TARGET={overlay.Name}/newcode SOURCES={overlay.Name}/source BUILD=build CODEADDR=0x{overlay.Address + overlay.Length:X7}",
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            Process p = new() { StartInfo = psi };
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += func;
            p.ErrorDataReceived += func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        private static bool CompileReplace(string subdir, string path, Overlay overlay)
        {
            uint address = uint.Parse(Path.GetFileNameWithoutExtension(subdir), NumberStyles.HexNumber);
            ProcessStartInfo psi = new()
            {
                FileName = "make",
                Arguments = $"TARGET={overlay.Name}/repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir}  NEWSYM={overlay.Name}/newcode.x BUILD=build CODEADDR=0x{address:X7}",
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            Process p = new() { StartInfo = psi };
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += func;
            p.ErrorDataReceived += func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }
    }
}
