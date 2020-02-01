using UnityEngine;

namespace BulletHell
{
	public class FoldoutAttribute : PropertyAttribute
	{
        public string Id;
		public string Name;
		public bool FoldEverything;
        
		public FoldoutAttribute(string name, bool foldEverything = false)
		{
			this.FoldEverything = foldEverything;
			this.Name = name;
		}
	}
}