#pragma strict

@MenuItem("Tools/ReplaceWithSpawner")
static function DoReplace()
{
	for (var go : GameObject in Selection.gameObjects) {
		var prefab = AssetDatabase.LoadMainAssetAtPath("Assets/Objects/"+go.name+".prefab") as GameObject;
		if (prefab != null)
		{
			var components = go.GetComponents(Component);
			for(var i = 1; i < components.Length; ++i)
			{
				GameObject.DestroyImmediate(components[i]);
			}
			var spawner = go.AddComponent(Spawner);
			spawner.target = prefab;
			
			var children = go.GetComponentsInChildren(Transform, true);
			
			for(var child : Transform in children)
			{
				if (child != null && child != go.transform)
				{
					GameObject.DestroyImmediate(child.gameObject);
				}
			}
		}
	}
}