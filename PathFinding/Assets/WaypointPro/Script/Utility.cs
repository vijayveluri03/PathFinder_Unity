using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
	public static GUIStyle GetStyleWithRichText ( GUIStyle style = null )
	{
		style = style != null ? style : new GUIStyle();
		style.richText = true;
		return style;
	}

}
