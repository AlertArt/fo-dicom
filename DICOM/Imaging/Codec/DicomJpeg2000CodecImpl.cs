// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;

using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;

namespace Dicom
{

    internal static class DicomJpeg2000CodecImpl
    {
        /*        private OPJ_COLOR_SPACE getOpenJpegColorSpace(PhotometricInterpretation photometricInterpretation)
        {
            if (photometricInterpretation == PhotometricInterpretation.Rgb) return CLRSPC_SRGB;
            else if (photometricInterpretation == PhotometricInterpretation.Monochrome1
                     || photometricInterpretation == PhotometricInterpretation.Monochrome2) return CLRSPC_GRAY;
            else if (photometricInterpretation == PhotometricInterpretation.PaletteColor) return CLRSPC_GRAY;
            else if (photometricInterpretation == PhotometricInterpretation.YbrFull
                     || photometricInterpretation == PhotometricInterpretation.YbrFull422
                     || photometricInterpretation == PhotometricInterpretation.YbrPartial422) return CLRSPC_SYCC;
            else return CLRSPC_UNKNOWN;
        }*/

        internal static void Encode(
            DicomPixelData oldPixelData,
            DicomPixelData newPixelData,
            DicomJpeg2000Params parameters)
        {
            if ((oldPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrFull422)
                || (oldPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrPartial422)
                || (oldPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrPartial420))
                throw new InvalidOperationException(
                    "Photometric Interpretation '" + oldPixelData.PhotometricInterpretation
                    + "' not supported by JPEG 2000 encoder");

            var jparams = parameters ?? new DicomJpeg2000Params();

            var pixelCount = oldPixelData.Height * oldPixelData.Width;

            /*            for (var frame = 0; frame < oldPixelData.NumberOfFrames; frame++)
            {
                var frameData = oldPixelData.GetFrame(frame);

                opj_image_cmptparm_t cmptparm[3];
                opj_cparameters_t eparams; // compression parameters
                opj_event_mgr_t event_mgr; // event manager
                opj_cinfo_t* cinfo = NULL; // handle to a compressor
                opj_image_t* image = NULL;
                opj_cio_t* cio = NULL;

                memset(&event_mgr, 0, sizeof(opj_event_mgr_t));
                event_mgr.error_handler = opj_error_callback;
                if (jparams.IsVerbose)
                {
                    event_mgr.warning_handler = opj_warning_callback;
                    event_mgr.info_handler = opj_info_callback;
                }

                cinfo = opj_create_compress(CODEC_J2K);

                opj_set_event_mgr((opj_common_ptr)cinfo, &event_mgr, NULL);

                opj_set_default_encoder_parameters(&eparams);
                eparams.cp_disto_alloc = 1;

                if (newPixelData.TransferSyntaxIsLossy && jparams.Irreversible) eparams.irreversible = 1;

                unsigned
                int r = 0;
                for (; r < jparams.RateLevels.Length; r++)
                {
                    if (jparams.RateLevels[r] > jparams.Rate)
                    {
                        eparams.tcp_numlayers++;
                        eparams.tcp_rates[r] = (float)jparams.RateLevels[r];
                    }
                    else break;
                }
                eparams.tcp_numlayers++;
                eparams.tcp_rates[r] = (float)jparams.Rate;

                if (!newPixelData.TransferSyntaxIsLossy && jparams.Rate > 0) eparams.tcp_rates[eparams.tcp_numlayers++] = 0;

                if (oldPixelData.PhotometricInterpretation == PhotometricInterpretation.Rgb && jparams.AllowMCT) eparams.tcp_mct = 1;

                memset(&cmptparm[0], 0, sizeof(opj_image_cmptparm_t) * 3);
                for (int i = 0; i < oldPixelData.SamplesPerPixel; i++)
                {
                    cmptparm[i].bpp = oldPixelData.BitsAllocated;
                    cmptparm[i].prec = oldPixelData.BitsStored;
                    if (!jparams.EncodeSignedPixelValuesAsUnsigned) cmptparm[i].sgnd = oldPixelData.PixelRepresentation == PixelRepresentation.Signed;
                    cmptparm[i].dx = eparams.subsampling_dx;
                    cmptparm[i].dy = eparams.subsampling_dy;
                    cmptparm[i].h = oldPixelData.Height;
                    cmptparm[i].w = oldPixelData.Width;
                }

                try
                {
                    OPJ_COLOR_SPACE color_space = getOpenJpegColorSpace(oldPixelData.PhotometricInterpretation);
                    image = opj_image_create(oldPixelData.SamplesPerPixel, &cmptparm[0], color_space);

                    image.x0 = eparams.image_offset_x0;
                    image.y0 = eparams.image_offset_y0;
                    image.x1 = image.x0 + ((oldPixelData.Width - 1) * eparams.subsampling_dx) + 1;
                    image.y1 = image.y0 + ((oldPixelData.Height - 1) * eparams.subsampling_dy) + 1;

                    for (int c = 0; c < image.numcomps; c++)
                    {
                        opj_image_comp_t* comp = &image.comps[c];

                        int pos = oldPixelData.PlanarConfiguration == PlanarConfiguration.Planar
                                      ? (c * pixelCount)
                                      : c;
                        const int offset =
                            oldPixelData.PlanarConfiguration == PlanarConfiguration.Planar ? 1 : image.numcomps;

                        if (oldPixelData.BytesAllocated == 1)
                        {
                            if (comp.sgnd)
                            {
                                if (oldPixelData.BitsStored < 8)
                                {
                                    const unsigned 
                                    char sign = 1 << oldPixelData.HighBit;
                                    const unsigned 
                                    char mask = (0xff >> (oldPixelData.BitsAllocated - oldPixelData.BitsStored));
                                    for (int p = 0; p < pixelCount; p++)
                                    {
                                        const unsigned 
                                        char pixel = frameData[pos];
                                        if (pixel & sign) comp.data[p] = -(((-pixel) & mask) + 1);
                                        else comp.data[p] = pixel;
                                        pos += offset;
                                    }
                                }
                                else
                                {
                                    char* frameData8 = (char*)(void*)frameData.begin();
                                    for (int p = 0; p < pixelCount; p++)
                                    {
                                        comp.data[p] = frameData8[pos];
                                        pos += offset;
                                    }
                                }
                            }
                            else
                            {
                                for (int p = 0; p < pixelCount; p++)
                                {
                                    comp.data[p] = frameData[pos];
                                    pos += offset;
                                }
                            }
                        }
                        else if (oldPixelData.BytesAllocated == 2)
                        {
                            if (comp.sgnd)
                            {
                                if (oldPixelData.BitsStored < 16)
                                {
                                    unsigned
                                    short* frameData16 = (unsigned
                                    short*  )
                                    (void*)frameData.begin();
                                    const unsigned 
                                    short sign = 1 << oldPixelData.HighBit;
                                    const unsigned 
                                    short mask = (0xffff >> (oldPixelData.BitsAllocated - oldPixelData.BitsStored));
                                    for (int p = 0; p < pixelCount; p++)
                                    {
                                        const unsigned 
                                        short pixel = frameData16[pos];
                                        if (pixel & sign) comp.data[p] = -(((-pixel) & mask) + 1);
                                        else comp.data[p] = pixel;
                                        pos += offset;
                                    }
                                }
                                else
                                {
                                    short* frameData16 = (short*)(void*)frameData.begin();
                                    for (int p = 0; p < pixelCount; p++)
                                    {
                                        comp.data[p] = frameData16[pos];
                                        pos += offset;
                                    }
                                }
                            }
                            else
                            {
                                unsigned
                                short* frameData16 = (unsigned
                                short*  )
                                (void*)frameData.begin();
                                for (int p = 0; p < pixelCount; p++)
                                {
                                    comp.data[p] = frameData16[pos];
                                    pos += offset;
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("JPEG 2000 codec only supports Bits Allocated == 8 or 16");
                        }
                    }

                    opj_setup_encoder(cinfo, &eparams, image);

                    cio = opj_cio_open((opj_common_ptr)cinfo, NULL, 0);

                    if (opj_encode(cinfo, cio, image, eparams.index))
                    {
                        int clen = cio_tell(cio);
                        Array<unsigned 
                        char >^
                        cbuf =ref
                        new Array<unsigned 
                        char >
                        (clen + ((clen & 1) == 1 ? 1 : 0));
                        Arrays.Copy(cio.buffer, cbuf, clen);

                        newPixelData.AddFrame(cbuf);
                    }
                    else throw ref
                    new InvalidOperationException("Unable to JPEG 2000 encode image");

                    if (cio != nullptr) opj_cio_close(cio);
                    if (image != nullptr) opj_image_destroy(image);
                    if (cinfo != nullptr) opj_destroy_compress(cinfo);
                }
                catch (...)
                {
                    if (cio != nullptr) opj_cio_close(cio);
                    if (image != nullptr) opj_image_destroy(image);
                    if (cinfo != nullptr) opj_destroy_compress(cinfo);

                    throw;
                }
            }

            if (oldPixelData.PhotometricInterpretation == PhotometricInterpretation.Rgb)
            {
                newPixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;

                if (jparams.AllowMCT && jparams.UpdatePhotometricInterpretation)
                {
                    if (newPixelData.TransferSyntaxIsLossy && jparams.Irreversible) newPixelData.PhotometricInterpretation = PhotometricInterpretation.YbrIct;
                    else newPixelData.PhotometricInterpretation = PhotometricInterpretation.YbrRct;
                }
            }*/
        }

        internal static void Decode(
            DicomPixelData oldPixelData,
            DicomPixelData newPixelData,
            DicomJpeg2000Params parameters)
        {
            var pixelCount = oldPixelData.Height * oldPixelData.Width;

            if (newPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrIct
                || newPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrRct) newPixelData.PhotometricInterpretation = PhotometricInterpretation.Rgb;

            if (newPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrFull422
                || newPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrPartial422) newPixelData.PhotometricInterpretation = PhotometricInterpretation.YbrFull;

            if (newPixelData.PhotometricInterpretation == PhotometricInterpretation.YbrFull) newPixelData.PlanarConfiguration = PlanarConfiguration.Planar;

            for (var frame = 0; frame < oldPixelData.NumberOfFrames; frame++)
            {
                var jpegData = oldPixelData.GetFrame(frame);

                // Destination frame should be of even length
                var frameSize = newPixelData.UncompressedFrameSize;
                if ((frameSize & 1) == 1) ++frameSize;
                var destArray = new byte[frameSize];

                var image = J2kImage.FromBytes(jpegData.Data, ToParameterList(parameters));

                if (image == null) throw new InvalidOperationException("Error in JPEG 2000 code stream!");

                for (int c = 0; c < image.numcomps; c++)
                {
                    var comp = image.comps[c];

                    var pos = newPixelData.PlanarConfiguration == PlanarConfiguration.Planar ? (c * pixelCount) : c;
                    var offset = newPixelData.PlanarConfiguration == PlanarConfiguration.Planar ? 1 : image.numcomps;

                    if (newPixelData.BytesAllocated == 1)
                    {
                        if (comp.sgnd)
                        {
                            var sign = (byte)(1 << newPixelData.HighBit);
                            var mask = (byte)(0xFF ^ sign);
                            for (var p = 0; p < pixelCount; p++)
                            {
                                var i = comp.data[p];
                                destArray[pos] = i < 0 ? (byte)((i & mask) | sign) : (byte)(i & mask);
                                pos += offset;
                            }
                        }
                        else
                        {
                            for (int p = 0; p < pixelCount; p++)
                            {
                                destArray[pos] = (byte)comp.data[p];
                                pos += offset;
                            }
                        }
                    }
                    else if (newPixelData.BytesAllocated == 2)
                    {
                        var sign = (ushort)(1 << newPixelData.HighBit);
                        var mask = (ushort)(0xFFFF ^ sign);
                        var destData16 = (ushort[])destArray;
                        if (comp.sgnd)
                        {
                            for (var p = 0; p < pixelCount; p++)
                            {
                                var i = comp.data[p];
                                destData16[pos] = i < 0 ? (ushort)((i & mask) | sign) : (ushort)(i & mask);
                                pos += offset;
                            }
                        }
                        else
                        {
                            for (int p = 0; p < pixelCount; p++)
                            {
                                destData16[pos] = (ushort)comp.data[p];
                                pos += offset;
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "JPEG 2000 module only supports Bytes Allocated == 8 or 16!");
                    }
                }

                newPixelData.AddFrame(new MemoryByteBuffer(destArray));
            }
        }

        private static ParameterList ToParameterList(DicomJpeg2000Params parameters)
        {
            throw new NotImplementedException();
        }
    }
}
