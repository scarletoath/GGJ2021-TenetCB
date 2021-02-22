using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.Utils
{
	public static class Extensions
	{

		public static string FullName(this GameObject GameObject) =>
			GameObject != null && GameObject.transform.parent != null ? $"{GameObject.transform.parent.gameObject.FullName()}.{GameObject.name}" : GameObject.name;

		public static string FullName(this Component Component) => Component != null ? Component.gameObject.FullName() : string.Empty;

	}
}