// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.Logging.Internal
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="M:string.Format"/>.
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal const int MaxCachedFormatters = 1024;
        private const string NullFormat = "[null]";
        private static int _count;
        private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
        internal readonly LogValuesFormatter Formatter;
        private readonly object[] _values;
        private readonly string _originalMessage;

        public FormattedLogValues(string format, params object[] values)
        {
            if (values?.Length != 0 && format != null)
            {
                if (_count >= MaxCachedFormatters)
                {
                    if (!_formatters.TryGetValue(format, out Formatter))
                    {
                        Formatter = new LogValuesFormatter(format);
                    }
                }
                else
                {
                    Formatter = _formatters.GetOrAdd(format, f =>
                    {
                        Interlocked.Increment(ref _count);
                        return new LogValuesFormatter(f);
                    });
                }
            }

            _originalMessage = format ?? NullFormat;
            _values = values;
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                if (index == Count - 1)
                {
                    return new KeyValuePair<string, object> ("{OriginalFormat}", _originalMessage);
                }

                return Formatter.GetValue(_values, index);
            }
        }

        public int Count
        {
            get
            {
                if (Formatter == null)
                {
                    return 1;
                }

                return Formatter.ValueNames.Count + 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        public override string ToString()
        {
            if (Formatter == null)
            {
                return _originalMessage;
            }

            return Formatter.Format(_values);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
