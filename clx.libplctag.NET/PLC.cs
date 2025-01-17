﻿using libplctag;
using libplctag.DataTypes;
using libplctag.NativeImport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace clx.libplctag.NET
{
    public class PLC : IDisposable
    {
        private readonly string _ipAddress = "192.168.1.196";
        private readonly string _path = "1,0";

        public int Timeout { get; set; } = 5;
        // public Response<string> _response { get; set; }
        // public Response<string> _failure { get; set; }
        private readonly ConcurrentDictionary<string, object> _tags = new ConcurrentDictionary<string, object>();
        public PLC(string ipAddress)
        {
            _ipAddress = ipAddress;
        }

        public PLC(string ipAddress, int slot)
        {
            _ipAddress = ipAddress;
            _path = "1," + slot.ToString();
        }
        
        public PLC(string ipAddress, string path)
        {
            _ipAddress = ipAddress;
            _path = path;
        }


        /// <summary>
        /// Reads a tag from the PLC.
        /// </summary>
        /// <param name="tagName">Tag name either a controller or program scoped tag</param>
        /// <param name="tagType">Tag type use TagType enum</param>
        /// <returns>Returns a tag value as string, regardless of tagType</returns>
        public async Task<Response<string>> Read(string tagName, TagType tagType)
        {
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:
                    // hack to read single array items for boolean arrays, broken downstream either at wrapper or libplctag core
                    var indexMatchBit = Regex.Match(tagName, @"(?<=\[).+?(?=\])");
                    if (indexMatchBit.Success)
                    {
                        var index = Int32.Parse(indexMatchBit.Value);
                        return await _ReadBoolArraySingle(tagName.Split(new[] {'['}, StringSplitOptions.None)[0], index).ConfigureAwait(false);
                    }

                    return await _ReadTag<BoolPlcMapper, bool>(tagName);
                case TagType.Dint:
                    return await _ReadTag<DintPlcMapper, int>(tagName);
                case TagType.Int:
                    return await _ReadTag<IntPlcMapper, short>(tagName);
                case TagType.Sint:
                    return await _ReadTag<SintPlcMapper, sbyte>(tagName);
                case TagType.Lint:
                    return await _ReadTag<LintPlcMapper, long>(tagName);
                case TagType.Real:
                    return await _ReadTag<RealPlcMapper, float>(tagName);
                case TagType.String:
                    return await _ReadTag<StringPlcMapper, string>(tagName);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<dynamic>> DRead(string tagName, TagType tagType)
        {
            return tagType switch
            {
                TagType.Bool => await _DReadTag<BoolPlcMapper, bool>(tagName).ConfigureAwait(false),
                TagType.Bit => await _DReadTag<BoolPlcMapper, bool>(tagName).ConfigureAwait(false),
                TagType.Dint => await _DReadTag<DintPlcMapper, int>(tagName).ConfigureAwait(false),
                TagType.Int => await _DReadTag<IntPlcMapper, short>(tagName).ConfigureAwait(false),
                TagType.Sint => await _DReadTag<SintPlcMapper, sbyte>(tagName).ConfigureAwait(false),
                TagType.Lint => await _DReadTag<LintPlcMapper, long>(tagName).ConfigureAwait(false),
                TagType.Real => await _DReadTag<RealPlcMapper, float>(tagName).ConfigureAwait(false),
                TagType.String => await _DReadTag<StringPlcMapper, string>(tagName).ConfigureAwait(false),
                _ => new Response<dynamic>(tagName, "Wrong Type"),
            };
        }


        /// <summary>
        /// Reads an array from the PLC.
        /// </summary>
        /// <param name="tagName">Tag name either a controller or program scoped tag</param>
        /// <param name="tagType">Tag type use TagType enum</param>
        /// <param name="arrayLength">Length of the array</param>
        /// <param name="startIndex">Optional starting index</param>
        /// <param name="count">Optional item count</param>
        /// <returns>Returns an array of strings, regardless of the tagType</returns>
        public async Task<Response<string[]>> Read(string tagName, TagType tagType, int arrayLength, int startIndex = 0, int count = 0)
        {
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:
                    var resultsBitArray = await ReadTag<BoolPlcMapper, bool[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsBitArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsBitArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }

                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }

                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsBitArray.Status);
                    }

                case TagType.Dint:
                    var resultsDintArray = await ReadTag<DintPlcMapper, int[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsDintArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsDintArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }

                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsDintArray.Status);
                    }

                case TagType.Int:
                    var resultsIntArray = await ReadTag<IntPlcMapper, short[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsIntArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsIntArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }
                        
                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsIntArray.Status);
                    }

                case TagType.Sint:
                    var resultsSintArray = await ReadTag<SintPlcMapper, sbyte[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsSintArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsSintArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }
                        
                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsSintArray.Status);
                    }

                case TagType.Lint:
                    var resultsLintArray = await ReadTag<LintPlcMapper, long[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsLintArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsLintArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }

                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsLintArray.Status);
                    }

                case TagType.Real:
                    var resultsRealArray = await ReadTag<RealPlcMapper, float[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsRealArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsRealArray.Value, Convert.ToString);
                        
                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }

                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsRealArray.Status);
                    }

                case TagType.String:
                    var resultsStringArray = await ReadTag<StringPlcMapper, string[]>(tagName, new int[] {arrayLength})
                        .ConfigureAwait(false);
                    if (resultsStringArray.Status == "Success")
                    {
                        string[] arrString = Array.ConvertAll(resultsStringArray.Value, Convert.ToString);

                        if (count > 0)
                        {
                            if (startIndex + count > arrayLength)
                            {
                                return new Response<string[]>(tagName, "MismatchLength");
                            }
                            
                            List<string> tagValueList = new List<string>(arrString);
                            return new Response<string[]>(tagName, tagValueList.GetRange(startIndex, count).ToArray(),
                                "Success");
                        }
                        
                        return new Response<string[]>(tagName, arrString, "Success");
                    }
                    else
                    {
                        return new Response<string[]>(tagName, resultsStringArray.Status);
                    }

                default:
                    return new Response<string[]>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<dynamic>> DRead(string tagName, TagType tagType, int arrayLength, int startIndex = 0, int count = 0)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Bool:
                    dynamic resultsBoolArray = await ReadTag<BoolPlcMapper, bool[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsBoolArray);

                case TagType.Bit:
                    dynamic resultsBitArray = await ReadTag<BoolPlcMapper, bool[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsBitArray);

                case TagType.Dint:
                    dynamic resultsDintArray = await ReadTag<DintPlcMapper, int[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsDintArray);

                case TagType.Int:
                    dynamic resultsIntArray = await ReadTag<IntPlcMapper, short[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsIntArray);

                case TagType.Sint:
                    dynamic resultsSintArray = await ReadTag<SintPlcMapper, sbyte[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsSintArray);

                case TagType.Lint:
                    dynamic resultsLintArray = await ReadTag<LintPlcMapper, long[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsLintArray);

                case TagType.Real:
                    dynamic resultsRealArray = await ReadTag<RealPlcMapper, float[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsRealArray);
                case TagType.String:
                    dynamic resultsStringArray = await ReadTag<StringPlcMapper, string[]>(tagName, new int[] { arrayLength }).ConfigureAwait(false);
                    return SendResponse(tagName, arrayLength, startIndex, count, resultsStringArray);

                default:
                    return new Response<dynamic>(tagName, "Failure");
            }
        }
        
        private static Response<dynamic> SendResponse(string tagName, int arrayLength, int startIndex, int count, dynamic resultsRealArray)
        {
            if (resultsRealArray.Status == "Success")
            {
                //string[] arrString = Array.ConvertAll(resultsRealArray.Value, Convert.ToString);
                if (startIndex + count > arrayLength)
                {
                    return new Response<dynamic>(tagName, "Failure, Out of bounds");
                }

                if (count > 0)
                {
                    //List<string> tagValueList = new List<string>(arrString);
                    return new Response<dynamic>(tagName, resultsRealArray.Value.Skip(startIndex + 1).Take(count).ToArray(), "Success");
                    //return new Response<dynamic>(tagName, resultsRealArray.Value.GetRange(startIndex, count).ToArray(), "Success");
                }
                return new Response<dynamic>(tagName, resultsRealArray.Value, "Success");
            }
            else
            {
                return new Response<dynamic>(tagName, "Failure");
            }
        }

        private async Task<Response<string>> _ReadTag<M, T>(string tagName) where M : IPlcMapper<T>, new()
        {
            var results = await ReadTag<M, T>(tagName).ConfigureAwait(false);
            if (results.Status == "Success")
            {
                return new Response<string>(tagName, results.Value.ToString(), "Success");
            }
            else
            {
                return new Response<string>(tagName, results.Status);
            }
        }
        
        private async Task<Response<dynamic>> _DReadTag<M, T>(string tagName) where M : IPlcMapper<T>, new()
        {
            var results = await ReadTag<M, T>(tagName).ConfigureAwait(false);
            if (results.Status == "Success")
            {
                return new Response<dynamic>(tagName, results.Value, "Success");
            }
            else
            {
                return new Response<dynamic>(tagName, results.Status);
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, object value)
        {
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:
                    // hack to write individual tags
                    var indexMatchBit = Regex.Match(tagName, @"(?<=\[).+?(?=\])");
                    if (indexMatchBit.Success)
                    {
                        var index = Int32.Parse(indexMatchBit.Value);
                        return await _WriteBoolArraySingle(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (bool) value, index)
                            .ConfigureAwait(false);
                    }

                    return await _WriteTag<BoolPlcMapper, bool>(tagName, (bool) value).ConfigureAwait(false);
                case TagType.Dint:
                    return await _WriteTag<DintPlcMapper, int>(tagName, (int) value).ConfigureAwait(false);
                case TagType.Int:
                    return await _WriteTag<IntPlcMapper, short>(tagName, (short) value).ConfigureAwait(false);
                case TagType.Sint:
                    return await _WriteTag<SintPlcMapper, sbyte>(tagName, (sbyte) value).ConfigureAwait(false);
                case TagType.Lint:
                    return await _WriteTag<LintPlcMapper, long>(tagName, (long) value).ConfigureAwait(false);
                case TagType.Real:
                    return await _WriteTag<RealPlcMapper, float>(tagName, (float) value).ConfigureAwait(false);
                case TagType.String:
                    return await _WriteTag<StringPlcMapper, string>(tagName, (string) value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        /*public async Task<Response<dynamic>> DWrite(string tagName, TagType tagType, object value)
        {
            return tagType switch
            {
                TagType.Bool => await _DWriteTag<BoolPlcMapper, bool>(tagName, (bool)value).ConfigureAwait(false),
                TagType.Bit => await _DWriteTag<BoolPlcMapper, bool>(tagName, (bool)value).ConfigureAwait(false),
                TagType.Dint => await _DWriteTag<DintPlcMapper, int>(tagName, (int)value).ConfigureAwait(false),
                TagType.Int => await _DWriteTag<IntPlcMapper, short>(tagName, (short)value).ConfigureAwait(false),
                TagType.Sint => await _DWriteTag<SintPlcMapper, sbyte>(tagName, (sbyte)value).ConfigureAwait(false),
                TagType.Lint => await _DWriteTag<LintPlcMapper, long>(tagName, (long)value).ConfigureAwait(false),
                TagType.Real => await _DWriteTag<RealPlcMapper, float>(tagName, (float)value).ConfigureAwait(false),
                TagType.String => await _DWriteTag<StringPlcMapper, string>(tagName, (string)value).ConfigureAwait(false),
                _ => new Response<dynamic>(tagName, "None", "Wrong Type"),
            };
        }*/

        private async Task<Response<dynamic>> _DWriteTag<M, T>(string tagName, T value) where M : IPlcMapper<T>, new()
        {
            var results = await WriteTag<M, T>(tagName, value).ConfigureAwait(false);

            if (results.Status == "Success")
            {
                return new Response<dynamic>(tagName, "Success");
            }
            else
            {
                return new Response<dynamic>(tagName, results.Status);
            }
        }


        public async Task<Response<string>> Write(string tagName, TagType tagType, bool value)
        {
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:
                    // hack to write individual tags
                    var indexMatchBit = Regex.Match(tagName, @"(?<=\[).+?(?=\])");
                    if (indexMatchBit.Success)
                    {
                        var index = Int32.Parse(indexMatchBit.Value);
                        return await _WriteBoolArraySingle(tagName.Split(new[] {'['}, StringSplitOptions.None)[0], value, index)
                            .ConfigureAwait(false);
                    }

                    return await _WriteTag<BoolPlcMapper, bool>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, int value)
        {
            switch (tagType)
            {
                case TagType.Dint:
                    return await _WriteTag<DintPlcMapper, int>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, short value)
        {
            switch (tagType)
            {
                case TagType.Int:
                    return await _WriteTag<IntPlcMapper, short>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, sbyte value)
        {
            switch (tagType)
            {
                case TagType.Sint:
                    return await _WriteTag<SintPlcMapper, sbyte>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, long value)
        {
            switch (tagType)
            {
                case TagType.Lint:
                    return await _WriteTag<LintPlcMapper, long>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, float value)
        {
            switch (tagType)
            {
                case TagType.Real:
                    return await _WriteTag<RealPlcMapper, float>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, string value)
        {
            switch (tagType)
            {
                case TagType.String:
                    return await _WriteTag<StringPlcMapper, string>(tagName, value).ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, object value, int arrayLength, int startIndex = 0, int count = 0)
        {
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:

                    if (count > 0)
                    {
                       return await WriteBoolArrayRange(tagName, (bool[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<BoolPlcMapper, bool[]>(tagName, (bool[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Dint:

                    if (count > 0)
                    {
                        return await WriteDintArrayRange(tagName, (int[]) value, arrayLength, startIndex);
                    }

                    return await WriteTag<DintPlcMapper, int[]>(tagName, (int[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Int:

                    if (count > 0)
                    {
                        return await WriteIntArrayRange(tagName, (short[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<IntPlcMapper, short[]>(tagName, (short[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Sint:

                    if (count > 0)
                    {
                        return await WriteSintArrayRange(tagName, (sbyte[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<SintPlcMapper, sbyte[]>(tagName, (sbyte[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Lint:

                    if (count > 0)
                    {
                       return await WriteLintArrayRange(tagName, (long[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<LintPlcMapper, long[]>(tagName, (long[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Real:

                    if (count > 0)
                    {
                        return await WriteRealArrayRange(tagName, (float[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<RealPlcMapper, float[]>(tagName, (float[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.String:

                    if (count > 0)
                    {
                       return await WriteStringArrayRange(tagName, (string[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<StringPlcMapper, string[]>(tagName, value as string[],
                            new int[] {arrayLength})
                        .ConfigureAwait(false);

                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<dynamic>> DWrite(string tagName, TagType tagType, object value, int arrayLength = 0, int startIndex = 0, int count = 0)
        {

            if (arrayLength == 0)
            {
                return tagType switch
                {
                    TagType.Bool => await _DWriteTag<BoolPlcMapper, bool>(tagName, (bool)value).ConfigureAwait(false),
                    TagType.Bit => await _DWriteTag<BoolPlcMapper, bool>(tagName, (bool)value).ConfigureAwait(false),
                    TagType.Dint => await _DWriteTag<DintPlcMapper, int>(tagName, (int)value).ConfigureAwait(false),
                    TagType.Int => await _DWriteTag<IntPlcMapper, short>(tagName, (short)value).ConfigureAwait(false),
                    TagType.Sint => await _DWriteTag<SintPlcMapper, sbyte>(tagName, (sbyte)value).ConfigureAwait(false),
                    TagType.Lint => await _DWriteTag<LintPlcMapper, long>(tagName, (long)value).ConfigureAwait(false),
                    TagType.Real => await _DWriteTag<RealPlcMapper, float>(tagName, (float)value).ConfigureAwait(false),
                    TagType.String => await _DWriteTag<StringPlcMapper, string>(tagName, (string)value).ConfigureAwait(false),
                    _ => new Response<dynamic>(tagName, "None", "Wrong Type"),
                };
            }
            
            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:

                    if (count > 0)
                    {
                        return await DWriteBoolArrayRange(tagName, (bool[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<BoolPlcMapper, bool[]>(tagName, (bool[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Dint:

                    if (count > 0)
                    {
                        return await DWriteDintArrayRange(tagName, (int[]) value, arrayLength, startIndex);
                    }

                    return await DWriteTag<DintPlcMapper, int[]>(tagName, (int[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Int:

                    if (count > 0)
                    {
                       return await DWriteIntArrayRange(tagName, (short[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<IntPlcMapper, short[]>(tagName, (short[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Sint:

                    if (count > 0)
                    {
                        return await DWriteSintArrayRange(tagName, (sbyte[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<SintPlcMapper, sbyte[]>(tagName, (sbyte[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Lint:

                    if (count > 0)
                    {
                       return await DWriteLintArrayRange(tagName, (long[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<LintPlcMapper, long[]>(tagName, (long[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.Real:

                    if (count > 0)
                    {
                        return await DWriteRealArrayRange(tagName, (float[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<RealPlcMapper, float[]>(tagName, (float[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                case TagType.String:

                    if (count > 0)
                    {
                        return await DWriteStringArrayRange(tagName, (string[]) value, arrayLength,
                            startIndex);
                    }

                    return await DWriteTag<StringPlcMapper, string[]>(tagName, value as string[],
                            new int[] {arrayLength})
                        .ConfigureAwait(false);

                default:
                    return new Response<dynamic>(tagName, "Wrong Type");
            }
        }


        public async Task<Response<string>> Write(string tagName, TagType tagType, bool[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Bool:
                case TagType.Bit:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteBoolArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (bool[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<BoolPlcMapper, bool[]>(tagName, (bool[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, int[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Dint:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteDintArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (int[]) value, arrayLength, startIndex);
                    }

                    return await WriteTag<DintPlcMapper, int[]>(tagName, (int[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, short[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Int:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteIntArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (short[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<IntPlcMapper, short[]>(tagName, (short[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);

                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, sbyte[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Sint:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteSintArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (sbyte[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<SintPlcMapper, sbyte[]>(tagName, (sbyte[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, long[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Lint:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteLintArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (long[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<LintPlcMapper, long[]>(tagName, (long[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, float[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.Real:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteRealArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (float[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<RealPlcMapper, float[]>(tagName, (float[]) value, new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }
        
        public async Task<Response<string>> Write(string tagName, TagType tagType, string[] value, int arrayLength)
        {
            var startIndexMatch = Regex.Match(tagName, @"(?<=\[).+?(?=\])");

            switch (tagType)
            {
                case TagType.String:

                    if (startIndexMatch.Success)
                    {
                        var startIndex = Int32.Parse(startIndexMatch.Value);
                        return await WriteStringArrayRange(tagName.Split(new[]{'['}, StringSplitOptions.None)[0], (string[]) value, arrayLength,
                            startIndex);
                    }

                    return await WriteTag<StringPlcMapper, string[]>(tagName, value as string[],
                            new int[] {arrayLength})
                        .ConfigureAwait(false);
                default:
                    return new Response<string>(tagName, "Wrong Type");
            }
        }

        private async Task<Response<string>> _WriteTag<M, T>(string tagName, T value) where M : IPlcMapper<T>, new()
        {
            var results = await WriteTag<M, T>(tagName, value).ConfigureAwait(false);

            if (results.Status == "Success")
            {
                return new Response<string>(tagName, "Success");
            }
            else
            {
                return new Response<string>(tagName, results.Status);
            }
        }

        public async Task<Response<T>> ReadTag<M, T>(string tagName) where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag = await GetOrAddTagToCacheAsync<M, T>(tagName);
            
            try
            {
                await tag.ReadAsync().ConfigureAwait(false);
                var tagValue = tag.Value;

                return new Response<T>(tagName, tagValue, "Success");
            }
            catch (Exception e)
            {
                return new Response<T>(tagName, e.Message);
            }
        }

        public async Task<Response<T>> ReadTag<M, T>(string tagName, int[] arrayDim = null)
            where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag = await GetOrAddTagToCacheAsync<M, T>(tagName);

            // Sanity Check, only support 3 dims in arrays for now
            if (arrayDim.Length > 3)
            {
                return new Response<T>(tagName, "InvalidArrayDim");
            }

            if (arrayDim.Length > 0 && arrayDim[0] > 0)
            {
                tag.ArrayDimensions = new int[] {arrayDim[0]};
                if (arrayDim.Length > 1 && arrayDim[1] > 0)
                {
                    tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1]};

                    if (arrayDim.Length > 2 && arrayDim[2] > 0)
                    {
                        tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1], arrayDim[2]};
                    }
                }
            }

            try
            {
                await tag.ReadAsync().ConfigureAwait(false);
                var tagValue = tag.Value;

                return new Response<T>(tagName, tagValue, "Success");
            }
            catch (Exception e)
            {
                return new Response<T>(tagName, e.Message);
            }
        }

        public async Task<Response<T>> WriteTag<M, T>(string tagName, T value) where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag = await GetOrAddTagToCacheAsync<M, T>(tagName);

            try
            {
                tag.Value = value;
                await tag.WriteAsync().ConfigureAwait(false);
                return new Response<T>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<T>(tagName, e.Message);
            }
        }

        public async Task<Response<string>> WriteTag<M, T>(string tagName, T value, int[] arrayDim = null)
            where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag = await GetOrAddTagToCacheAsync<M, T>(tagName);

            // Sanity Check, only support 3 dims in arrays for now
            if (arrayDim.Length > 3)
            {
                return new Response<string>(tagName, "InvalidArrayDim");
            }

            if (arrayDim.Length > 0 && arrayDim[0] > 0)
            {
                tag.ArrayDimensions = new int[] {arrayDim[0]};
                if (arrayDim.Length > 1 && arrayDim[1] > 0)
                {
                    tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1]};

                    if (arrayDim.Length > 2 && arrayDim[2] > 0)
                    {
                        tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1], arrayDim[2]};
                    }
                }
            }

            try
            {
                tag.Value = value;
                await tag.WriteAsync().ConfigureAwait(false);
                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }
        
        public async Task<Response<dynamic>> DWriteTag<M, T>(string tagName, T value, int[] arrayDim = null)
            where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag = await GetOrAddTagToCacheAsync<M, T>(tagName);

            // Sanity Check, only support 3 dims in arrays for now
            if (arrayDim.Length > 3)
            {
                return new Response<dynamic>(tagName, "InvalidArrayDim");
            }

            if (arrayDim.Length > 0 && arrayDim[0] > 0)
            {
                tag.ArrayDimensions = new int[] {arrayDim[0]};
                if (arrayDim.Length > 1 && arrayDim[1] > 0)
                {
                    tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1]};

                    if (arrayDim.Length > 2 && arrayDim[2] > 0)
                    {
                        tag.ArrayDimensions = new int[] {arrayDim[0], arrayDim[1], arrayDim[2]};
                    }
                }
            }

            try
            {
                tag.Value = value;
                await tag.WriteAsync().ConfigureAwait(false);
                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }


        private async Task<Response<string>> WriteDintArrayRange(string tagName, int[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt32((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> WriteIntArrayRange(string tagName, short[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt16((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> WriteSintArrayRange(string tagName, sbyte[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt8((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> WriteLintArrayRange(string tagName, long[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt64((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> WriteRealArrayRange(string tagName, float[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetFloat32((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> WriteBoolArrayRange(string tagName, bool[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength / 32;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                //var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetBit(startIndex + i, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }
        
        private async Task<Response<string>> WriteStringArrayRange(string tagName, string[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            const int MAX_CONTROLLOGIX_STRING_LENGTH = 88;
            const int LEN_OFFSET = 0;
            const int DATA_OFFSET = 4;

            // sanity checks
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<string>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength; // should always be 88

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i].Length > MAX_CONTROLLOGIX_STRING_LENGTH)
                    {
                        return new Response<string>(tagName, $"String {value[i]} exceeds maximum length");
                    }

                    var actualStringLength = Math.Min(value[i].Length, MAX_CONTROLLOGIX_STRING_LENGTH);
                    tag.SetInt16((startIndex + i) * offsetSize + LEN_OFFSET, Convert.ToInt16(value[i].Length));

                    byte[] asciiEncodedString = new byte[actualStringLength];
                    Encoding.ASCII.GetBytes(value[i]).CopyTo(asciiEncodedString, 0);
                    
                    for (int ii = 0; ii < asciiEncodedString.Length; ii++)
                    {
                        tag.SetUInt8((startIndex + i) * offsetSize + DATA_OFFSET + ii, asciiEncodedString[ii]);
                       
                    }
                    //await tag.WriteAsync().ConfigureAwait(false);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> _WriteBoolArraySingle(string tagName, bool value, int index)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = (index / 32) + 1;

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                tag.SetBit(index, value);
                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<string>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }

        private async Task<Response<string>> _ReadBoolArraySingle(string tagName, int index)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = (index / 32) + 1;

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var tagValue = tag.GetBit(index);
                tag.Dispose();

                return new Response<string>(tagName, tagValue.ToString(), "Success");
            }
            catch (Exception e)
            {
                return new Response<string>(tagName, e.Message);
            }
        }
        
        private async Task<Response<dynamic>> DWriteDintArrayRange(string tagName, int[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt32((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Response<dynamic>> DWriteIntArrayRange(string tagName, short[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt16((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Response<dynamic>> DWriteSintArrayRange(string tagName, sbyte[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt8((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Response<dynamic>> DWriteLintArrayRange(string tagName, long[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetInt64((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Response<dynamic>> DWriteRealArrayRange(string tagName, float[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetFloat32((startIndex + i) * offsetSize, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Response<dynamic>> DWriteBoolArrayRange(string tagName, bool[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength / 32;

            // sanity check
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                //var offsetSize = tag.GetSize() / arrayLength;

                for (int i = 0; i < value.Length; i++)
                {
                    tag.SetBit(startIndex + i, value[i]);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }
        
        private async Task<Response<dynamic>> DWriteStringArrayRange(string tagName, string[] value, int arrayLength,
            int startIndex)
        {
            var tag = new Tag();
            tag.Name = tagName;
            tag.Gateway = _ipAddress;
            tag.Path = _path;
            tag.PlcType = PlcType.ControlLogix;
            tag.Protocol = Protocol.ab_eip;
            tag.Timeout = TimeSpan.FromSeconds(Timeout);
            tag.ElementCount = arrayLength;

            const int MAX_CONTROLLOGIX_STRING_LENGTH = 88;
            const int LEN_OFFSET = 0;
            const int DATA_OFFSET = 4;

            // sanity checks
            if (startIndex + value.Length > arrayLength)
            {
                return new Response<dynamic>(tagName, "MismatchLength");
            }

            try
            {
                await tag.InitializeAsync().ConfigureAwait(false);
                var offsetSize = tag.GetSize() / arrayLength; // should always be 88

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i].Length > MAX_CONTROLLOGIX_STRING_LENGTH)
                    {
                        return new Response<dynamic>(tagName, $"String {value[i]} exceeds maximum length");
                    }

                    var actualStringLength = Math.Min(value[i].Length, MAX_CONTROLLOGIX_STRING_LENGTH);
                    tag.SetInt16((startIndex + i) * offsetSize + LEN_OFFSET, Convert.ToInt16(value[i].Length));

                    byte[] asciiEncodedString = new byte[actualStringLength];
                    Encoding.ASCII.GetBytes(value[i]).CopyTo(asciiEncodedString, 0);
                    
                    for (int ii = 0; ii < asciiEncodedString.Length; ii++)
                    {
                        tag.SetUInt8((startIndex + i) * offsetSize + DATA_OFFSET + ii, asciiEncodedString[ii]);
                       
                    }
                    //await tag.WriteAsync().ConfigureAwait(false);
                }

                await tag.WriteAsync().ConfigureAwait(false);
                tag.Dispose();

                return new Response<dynamic>(tagName, "Success");
            }
            catch (Exception e)
            {
                return new Response<dynamic>(tagName, e.Message);
            }
        }

        private async Task<Tag<M, T>> GetOrAddTagToCacheAsync<M, T>(string tagName, CancellationToken cancellationToken = default)
            where M : IPlcMapper<T>, new()
        {
            Tag<M, T> tag;
            if (_tags.TryGetValue(tagName, out var temp))
                tag = temp as Tag<M, T>;
            else
            {
                Console.WriteLine("Creating {0} for {1}", tagName, _ipAddress);
                tag = _tags.GetOrAdd(tagName, key => new Tag<M, T>()
                {
                    Name = key,
                    Gateway = _ipAddress,
                    Path = _path,
                    PlcType = PlcType.ControlLogix,
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromSeconds(Timeout)
                }) as Tag<M, T>;
                if (tag is not null)
                    await tag.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            return tag;
        }
        public void ReleaseTagIfExists(string tagName)
        {
            if (!_tags.TryRemove(tagName, out var temp)) return;
            if (temp is IDisposable disposable)
                disposable.Dispose();
        }
        
        public void Dispose()
        {
            foreach (var key in _tags.Keys)
            {
                if (_tags[key] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}