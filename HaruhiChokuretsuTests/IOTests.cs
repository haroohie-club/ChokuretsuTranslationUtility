using HaruhiChokuretsuLib.Util;
using NUnit.Framework;

namespace HaruhiChokuretsuTests
{
    public class IOTests
    {
        [Test]
        public void BitReaderTest()
        {
            byte[] bytes = [0xAA, 0xAB, 0xA5, 0x80, 0xFF, 0xFF, 0x00, 0x00];

            Assert.That(BigEndianIO.ReadBits(bytes, 0, 0, 16), Is.EqualTo(0xAAAB));
            Assert.That(BigEndianIO.ReadBits(bytes, 0, 16, 4), Is.EqualTo(0x0A));
            Assert.That(BigEndianIO.ReadBits(bytes, 0, 20, 4), Is.EqualTo(0x05));
            Assert.That(BigEndianIO.ReadBits(bytes, 0, 24, 2), Is.EqualTo(0x02));
            Assert.That(BigEndianIO.ReadBits(bytes, 0, 26, 8), Is.EqualTo(0x03));
            Assert.That(BigEndianIO.ReadBits(bytes, 0, 34, 30), Is.EqualTo(0x3FFF0000));
        }
    }
}
