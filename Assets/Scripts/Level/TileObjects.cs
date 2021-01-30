using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tenet.Utils.Editor;
using UnityEngine;

namespace Tenet.Level
{
    public class TileObjects : MonoBehaviour
    {
        [SerializeField, Tag(TagCategory.GeneralTag)] private string[] GeneralTags = Array.Empty<string>();
        [SerializeField, Tag(TagCategory.ReservedTag)] private string[] ReservedTags = Array.Empty<string>();

        public IEnumerable<string> GetGeneralTags() => GeneralTags;
        public IEnumerable<string> GetReservedTags() => ReservedTags;
        public IEnumerable<string> GetAllTags() => GeneralTags.Concat(ReservedTags);

#if UNITY_EDITOR
		private void OnValidate() => LevelGenerator.RefreshTileObjectsLibrary();
#endif
	}
}