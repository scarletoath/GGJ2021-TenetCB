using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Utils.Editor;
using UnityEngine;

namespace Tenet.Level
{
    public class TilePattern : MonoBehaviour
    {
        [SerializeField, Tag(TagCategory.Difficulty)] private string _Difficulty;
		public string Difficulty => _Difficulty;

#if UNITY_EDITOR
		private void OnValidate() => DifficultySettings.RefreshTilePatternsLibrary();
#endif
	}
}