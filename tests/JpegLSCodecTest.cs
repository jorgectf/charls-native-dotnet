// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CharLS.Native;
using NUnit.Framework;

namespace CharLS.Test
{
    [TestFixture]
    public class JpegLSCodecTest
    {
        [Test]
        public void GetMetadataInfoFromLosslessEncodedColorImage()
        {
            var source = ReadAllBytes("T8C0E0.JLS");

            var decoder = new JpegLSDecoder(source);
            decoder.ReadHeader();

            var frameInfo = decoder.FrameInfo;

            Assert.AreEqual(frameInfo.Height, 256);
            Assert.AreEqual(frameInfo.Width, 256);
            Assert.AreEqual(frameInfo.BitsPerSample, 8);
            Assert.AreEqual(frameInfo.ComponentCount, 3);
            Assert.AreEqual(decoder.NearLossless, 0);
        }

        [Test]
        public void GetMetadataInfoFromNearLosslessEncodedColorImage()
        {
            var source = ReadAllBytes("T8C0E3.JLS");

            var decoder = new JpegLSDecoder(source);
            decoder.ReadHeader();

            var frameInfo = decoder.FrameInfo;

            Assert.AreEqual(frameInfo.Height, 256);
            Assert.AreEqual(frameInfo.Width, 256);
            Assert.AreEqual(frameInfo.BitsPerSample, 8);
            Assert.AreEqual(frameInfo.ComponentCount, 3);
            Assert.AreEqual(decoder.NearLossless, 3);
        }

        [Test]
        public void Decode()
        {
            var source = ReadAllBytes("T8C0E0.JLS");
            var expected = ReadAllBytes("TEST8.PPM", 15);
            var uncompressed = JpegLSDecoder.Decode(source);

            var decoder = new JpegLSDecoder(source);
            decoder.ReadHeader();

            var frameInfo = decoder.FrameInfo;
            if (decoder.InterleaveMode == JpegLSInterleaveMode.None && frameInfo.ComponentCount == 3)
            {
                expected = TripletToPlanar(expected, (int)frameInfo.Width, (int)frameInfo.Height);
            }

            Assert.AreEqual(expected, uncompressed);
        }

        [Test]
        public void Encode()
        {
            var info = new FrameInfo(256, 256, 8, 3);

            var uncompressedOriginal = ReadAllBytes("TEST8.PPM", 15);
            uncompressedOriginal = TripletToPlanar(uncompressedOriginal, info.Width, info.Height);

            var encoder = new JpegLSEncoder { FrameInfo = info };

            var encoded = new byte[encoder.EstimatedDestinationSize];
            encoder.SetDestination(encoded);
            encoder.Encode(uncompressedOriginal);

            Array.Resize(ref encoded, (int)encoder.BytesWritten);

            var decoder = new JpegLSDecoder(encoded);
            decoder.ReadHeader();
            ////var compressedInfo = JpegLSCodec.GetMetadataInfo(compressed);
            ////Assert.AreEqual(info, compressedInfo);

            var uncompressed = decoder.Decode();
            Assert.AreEqual(uncompressedOriginal.Length, uncompressed.Length);
            Assert.AreEqual(uncompressedOriginal, uncompressed);
        }

        [Test]
        public void CompressPartOfInputBuffer()
        {
            var info = new JpegLSMetadataInfo(256, 256, 8, 3);

            var uncompressedOriginal = ReadAllBytes("TEST8.PPM", 15);
            uncompressedOriginal = TripletToPlanar(uncompressedOriginal, info.Width, info.Height);

            var compressedSegment = JpegLSCodec.Compress(info, uncompressedOriginal, uncompressedOriginal.Length);
            var compressed = new byte[compressedSegment.Count];
            Array.Copy(compressedSegment.Array, compressed, compressed.Length);

            var compressedInfo = JpegLSCodec.GetMetadataInfo(compressed);
            Assert.AreEqual(info, compressedInfo);

            var uncompressed = JpegLSCodec.Decompress(compressed);
            Assert.AreEqual(info.UncompressedSize, uncompressed.Length);
            Assert.AreEqual(uncompressedOriginal, uncompressed);
        }

        [Test]
        public void CompressOneByOneColor()
        {
            var uncompressedOriginal = new byte[] { 77, 33, 255 };
            var encoder = new JpegLSEncoder { FrameInfo = new FrameInfo(1, 1, 8, 3) };

            var encoded = new byte[encoder.EstimatedDestinationSize];
            encoder.SetDestination(encoded);
            encoder.Encode(uncompressedOriginal);

            var decoder = new JpegLSDecoder(encoded);
            decoder.ReadHeader();

            var uncompressed = decoder.Decode();
            Assert.AreEqual(uncompressedOriginal.Length, uncompressed.Length);
            Assert.AreEqual(uncompressedOriginal, uncompressed);
        }

        [Test]
        public void Compress2BitMonochrome()
        {
            var uncompressedOriginal = new byte[] { 1 };
            var encoder = new JpegLSEncoder { FrameInfo = new FrameInfo(1, 1, 2, 1) };

            var encoded = new byte[encoder.EstimatedDestinationSize];
            encoder.SetDestination(encoded);
            encoder.Encode(uncompressedOriginal);

            var decoder = new JpegLSDecoder(encoded);
            decoder.ReadHeader();

            var uncompressed = decoder.Decode();
            Assert.AreEqual(uncompressedOriginal.Length, uncompressed.Length);
            Assert.AreEqual(uncompressedOriginal, uncompressed);
        }

        [Test]
        public void DecodeBitStreamWithNoMarkerStart()
        {
            var source = new byte[] { 0x33, 0x33 };

            var exception = Assert.Throws<InvalidDataException>(() => JpegLSDecoder.Decode(source));
            Assert.AreEqual(JpegLSError.JpegMarkerStartByteNotFound, exception.Data["JpegLSError"]);
        }

        [Test]
        public void DecodeBitStreamWithUnsupportedEncoding()
        {
            var source = new byte[]
                {
                    0xFF, 0xD8, // Start Of Image (JPEG_SOI)
                    0xFF, 0xC3, // Start Of Frame (lossless, Huffman) (JPEG_SOF_3)
                    0x00, 0x00 // Length of data of the marker
                };
            var exception = Assert.Throws<InvalidDataException>(() => JpegLSDecoder.Decode(source));
            Assert.AreEqual(JpegLSError.EncodingNotSupported, exception.Data["JpegLSError"]);
        }

        [Test]
        public void DecodeBitStreamWithUnknownJpegMarker()
        {
            var source = new byte[]
                {
                    0xFF, 0xD8, // Start Of Image (JPEG_SOI)
                    0xFF, 0x01, // Undefined marker
                    0x00, 0x00 // Length of data of the marker
                };

            var exception = Assert.Throws<InvalidDataException>(() => JpegLSDecoder.Decode(source));
            Assert.AreEqual(JpegLSError.UnknownJpegMarkerFound, exception.Data["JpegLSError"]);
        }

        private static byte[] TripletToPlanar(IList<byte> buffer, int width, int height)
        {
            var result = new byte[buffer.Count];

            int bytePlaneCount = width * height;
            for (int i = 0; i < bytePlaneCount; i++)
            {
                result[i] = buffer[i * 3];
                result[i + bytePlaneCount] = buffer[(i * 3) + 1];
                result[i + (2 * bytePlaneCount)] = buffer[(i * 3) + 2];
            }

            return result;
        }

        private static byte[] ReadAllBytes(string path, int bytesToSkip = 0)
        {
            var fullPath = DataFileDirectory + path;

            if (bytesToSkip == 0)
                return File.ReadAllBytes(fullPath);

            using var stream = File.OpenRead(fullPath);
            var result = new byte[new FileInfo(fullPath).Length - bytesToSkip];

            stream.Seek(bytesToSkip, SeekOrigin.Begin);
            stream.Read(result, 0, result.Length);
            return result;
        }

        private static string DataFileDirectory
        {
            get
            {
                var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().CodeBase);
                return Path.GetDirectoryName(assemblyLocation.LocalPath) + @"\DataFiles\";
            }
        }
    }
}
