// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;

namespace CharLS.Native
{
    /// <summary>
    /// JPEG-LS Encoder.
    /// </summary>
    public sealed class JpegLSEncoder : IDisposable
    {
        private readonly SafeHandleJpegLSEncoder _encoder = CreateEncoder();
        private FrameInfo? _frameInfo;
        private int _nearLossless;

        /// <summary>
        /// Gets or sets the frame information.
        /// </summary>
        /// <value>
        /// The frame information.
        /// </value>
        public FrameInfo? FrameInfo
        {
            get
            {
                return _frameInfo;
            }

            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                FrameInfoNative infoNative = default;

                infoNative.Height = (uint)value.Height;
                infoNative.Width = (uint)value.Width;
                infoNative.BitsPerSample = value.BitsPerSample;
                infoNative.ComponentCount = value.ComponentCount;

                var error = Environment.Is64BitProcess
                    ? SafeNativeMethods.CharLSSetFrameInfoX64(_encoder, ref infoNative)
                    : SafeNativeMethods.CharLSSetFrameInfoX86(_encoder, ref infoNative);
                JpegLSCodec.HandleResult(error);

                _frameInfo = value;
            }
        }

        /// <summary>
        /// Gets or sets the near lossless parameter used to encode the JPEG-LS stream.
        /// </summary>
        /// <value>
        /// The near lossless parameter value.
        /// </value>
        public int NearLossless
        {
            get
            {
                return _nearLossless;
            }

            set
            {
                var error = Environment.Is64BitProcess
                    ? SafeNativeMethods.CharLSSetNearLosslessX64(_encoder, value)
                    : SafeNativeMethods.CharLSSetNearLosslessX86(_encoder, value);
                JpegLSCodec.HandleResult(error);

                _nearLossless = value;
            }
        }

        /// <summary>
        /// Gets the size of the estimated destination.
        /// </summary>
        /// <value>
        /// The size of the estimated destination.
        /// </value>
        public long EstimatedDestinationSize
        {
            get
            {
                IntPtr sizeInBytes;

                var error = Environment.Is64BitProcess
                    ? SafeNativeMethods.CharLSGetEstimatedDestinationSizeX64(_encoder, out sizeInBytes)
                    : SafeNativeMethods.CharLSGetEstimatedDestinationSizeX86(_encoder, out sizeInBytes);
                JpegLSCodec.HandleResult(error);

                return (long)sizeInBytes;
            }
        }

        /// <summary>
        /// Gets the bytes written.
        /// </summary>
        /// <value>
        /// The bytes written.
        /// </value>
        public long BytesWritten
        {
            get
            {
                IntPtr bytesWritten;

                var error = Environment.Is64BitProcess
                    ? SafeNativeMethods.CharLSGetBytesWrittenX64(_encoder, out bytesWritten)
                    : SafeNativeMethods.CharLSGetBytesWrittenX86(_encoder, out bytesWritten);
                JpegLSCodec.HandleResult(error);

                return (long)bytesWritten;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _encoder.Dispose();
        }

        /// <summary>
        /// Sets the destination buffer that contains the pixels that need to be encoded.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="destinationLength">Length of the destination buffer, when 0 .</param>
        public void SetDestination(byte[] destination, int destinationLength = 0)
        {
            if (destinationLength == 0)
            {
                destinationLength = destination.Length;
            }

            var error = Environment.Is64BitProcess
                ? SafeNativeMethods.CharLSSetDestinationBufferX64(_encoder, destination, (uint)destinationLength)
                : SafeNativeMethods.CharLSSetDestinationBufferX86(_encoder, destination, (uint)destinationLength);
            JpegLSCodec.HandleResult(error);
        }

        /// <summary>
        /// Encodes the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceLength">Length of the source.</param>
        /// <param name="stride">The stride.</param>
        public void Encode(byte[] source, int sourceLength = 0, int stride = 0)
        {
            if (sourceLength == 0)
            {
                sourceLength = source.Length;
            }

            var error = Environment.Is64BitProcess
                ? SafeNativeMethods.CharLSEncodeFromBufferX64(_encoder, source, (IntPtr)sourceLength, (uint)stride)
                : SafeNativeMethods.CharLSEncodeFromBufferX86(_encoder, source, (IntPtr)sourceLength, (uint)stride);
            JpegLSCodec.HandleResult(error);
        }

        /// <summary>
        /// Writes a standard SPIFF header to the destination. The additional values are computed from the current encoder settings.
        /// A SPIFF header is optional, but recommended for standalone JPEG-LS files.
        /// </summary>
        /// <param name="colorSpace">The color space of the image.</param>
        /// <param name="resolutionUnit">The resolution units of the next 2 parameters.</param>
        /// <param name="verticalResolution">The vertical resolution.</param>
        /// <param name="horizontalResolution">The horizontal resolution.</param>
        public void WriteStandardSpiffHeader(SpiffColorSpace colorSpace, SpiffResolutionUnit resolutionUnit = SpiffResolutionUnit.AspectRatio,
            int verticalResolution = 1, int horizontalResolution = 1)
        {
            var error = Environment.Is64BitProcess
                ? SafeNativeMethods.CharLSWriteStandardSpiffHeaderX64(_encoder, colorSpace, resolutionUnit, (uint)verticalResolution, (uint)horizontalResolution)
                : SafeNativeMethods.CharLSWriteStandardSpiffHeaderX86(_encoder, colorSpace, resolutionUnit, (uint)verticalResolution, (uint)horizontalResolution);
            JpegLSCodec.HandleResult(error);
        }

        /// <summary>
        /// Writes a SPIFF header to the destination.
        /// </summary>
        /// <param name="spiffHeader">Reference to a SPIFF header that will be written to the destination.</param>
        public void WriteSpiffHeader(SpiffHeader spiffHeader)
        {
            var headerNative = new SpiffHeaderNative(spiffHeader);

            var error = Environment.Is64BitProcess
                ? SafeNativeMethods.CharLSWriteSpiffHeaderX64(_encoder, ref headerNative)
                : SafeNativeMethods.CharLSWriteSpiffHeaderX86(_encoder, ref headerNative);
            JpegLSCodec.HandleResult(error);
        }

        private static SafeHandleJpegLSEncoder CreateEncoder()
        {
            var encoder = Environment.Is64BitProcess
                ? SafeNativeMethods.CharLSCreateEncoderX64()
                : SafeNativeMethods.CharLSCreateEncoderX86();
            if (encoder.IsInvalid)
                throw new OutOfMemoryException();

            return encoder;
        }
    }
}
