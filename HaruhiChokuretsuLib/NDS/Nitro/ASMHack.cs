using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace HaruhiChokuretsuLib.NDS.Nitro
{
	public class ASMHack
	{
		public static bool Insert(string path, ARM9 arm9, uint arenaLoOffset)
		{
			uint arenaLo = arm9.ReadU32LE(arenaLoOffset);
            if (!Compile(path, arenaLo))
            {
                Console.ReadLine();
                return false;
            }
			if (!File.Exists(Path.Combine(path, "newcode.bin")))
			{
				return false;
			}
			byte[] newCode = File.ReadAllBytes(Path.Combine(path, "newcode.bin"));

			StreamReader r = new(Path.Combine(path, "newcode.sym"));
			string currentLine;
			while ((currentLine = r.ReadLine()) != null)
			{
				string[] lines = currentLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (lines.Length == 4)
				{
					if (lines[3].Length < 7) continue;
					switch (lines[3].Remove(6))
					{
						case "arepl_":
							{
								string replaceOffsetString = lines[3].Replace("arepl_", "");
								uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
								uint replace = 0xEB000000; //BL Instruction
								uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
								relativeDestinationOffset &= 0x00FFFFFF;
								replace |= relativeDestinationOffset;
                                if (!arm9.WriteU32LE(replaceOffset, replace))
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                                        );
                                }
								break;
							}
						case "ansub_":
							{
								string replaceOffsetString = lines[3].Replace("ansub_", "");
								uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
								uint replace = 0xEA000000;//B Instruction
								uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
								relativeDestinationOffset &= 0x00FFFFFF;
								replace |= relativeDestinationOffset;
                                if (!arm9.WriteU32LE(replaceOffset, replace))
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                                        );
                                }
								break;
							}
						case "trepl_":
							{
								string ReplaceOffsetString = lines[3].Replace("trepl_", "");
								uint ReplaceOffset = uint.Parse(ReplaceOffsetString, NumberStyles.HexNumber);
								ushort Replace1 = 0xF000;//BLX Instruction (Part 1)
								ushort Replace2 = 0xE800;//BLX Instruction (Part 2)
								uint DestinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint RelativeDestinationOffset = DestinationOffset - ReplaceOffset - 2;
								RelativeDestinationOffset >>= 1;
								RelativeDestinationOffset &= 0x003FFFFF;
								Replace1 |= (ushort)((RelativeDestinationOffset >> 11) & 0x7FF);
								Replace2 |= (ushort)((RelativeDestinationOffset >> 0) & 0x7FE);
								if (!arm9.WriteU16LE(ReplaceOffset, Replace1)) 
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset.\r\nIf your code is inside an overlay, this is an action replay code to let your asm hack still work:\r\n1 {ReplaceOffset:X7} 0000{Replace1:X4}\r\n1{ReplaceOffset + 2:X7} 0000{Replace2:X4})"
                                        );
                                }
								else arm9.WriteU16LE(ReplaceOffset + 2, Replace2);
								break;
							}
					}
				}
			}
			r.Close();
			arm9.WriteU32LE(arenaLoOffset, arenaLo + (uint)newCode.Length);
			arm9.AddAutoLoadEntry(arenaLo, newCode);
			File.Delete(Path.Combine(path, "newcode.bin"));
			File.Delete(Path.Combine(path, "newcode.elf"));
			File.Delete(Path.Combine(path, "newcode.sym"));
			Directory.Delete(Path.Combine(path, "build"), true);
			return true;
		}

		private static bool Compile(string path, uint arenaLo)
		{
			ProcessStartInfo psi = new()
			{
				FileName = "make",
				Arguments = $"CODEADDR=0x{arenaLo:X8}",
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
