using YaeBlog.Extensions;
using YaeBlog.Models;

namespace YaeBlog.Services
{
    public class MarkdownWordCounter
    {
        private bool _inCodeBlock;
        private int _index;
        private readonly string _content;

        private uint WordCount { get; set; }

        private MarkdownWordCounter(BlogContent content)
        {
            _content = content.Content;
        }

        private void CountWordInner()
        {
            while (_index < _content.Length)
            {
                if (IsCodeBlockTag())
                {
                    _inCodeBlock = !_inCodeBlock;
                }

                if (!_inCodeBlock && char.IsLetterOrDigit(_content, _index))
                {
                    WordCount += 1;
                }

                _index++;
            }
        }

        private bool IsCodeBlockTag()
        {
            // 首先考虑识别代码块
            bool outerCodeBlock =
                Enumerable.Range(0, 3)
                .Select(i => _index + i < _content.Length && _content.AsSpan().Slice(_index + i, 1) is "`")
                .All(i => i);

            if (outerCodeBlock)
            {
                return true;
            }

            // 然后识别行内代码
            return _index < _content.Length && _content.AsSpan().Slice(_index, 1) is "`";
        }

        public static uint CountWord(BlogContent content)
        {
            MarkdownWordCounter counter = new(content);
            counter.CountWordInner();

            return counter.WordCount;
        }
    }
}
