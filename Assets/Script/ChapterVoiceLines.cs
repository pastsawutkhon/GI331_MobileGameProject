using System;
using UnityEngine;

namespace Solution
{
    public class ChapterVoiceLines : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            public string id, speaker, text;
        }

        public Font font;
        public Line[] lines;
    }
}
