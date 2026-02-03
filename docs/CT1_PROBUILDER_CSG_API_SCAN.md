# CT-1: ProBuilder CSG API Scan Report

- Unity: `6000.2.2f1`
- Generated: `2025-12-15 13:32:36`
- Project root: `(omitted)`

## Scripting Define Symbols
```

```

## ProBuilder-related assemblies (loaded)
- `Unity.ProBuilder`
- `Unity.ProBuilder.AddOns.Editor`
- `Unity.ProBuilder.AssetIdRemapUtility`
- `Unity.ProBuilder.Csg`
- `Unity.ProBuilder.Editor`
- `Unity.ProBuilder.KdTree`
- `Unity.ProBuilder.Poly2Tri`
- `Unity.ProBuilder.Stl`

## Assembly: `Unity.ProBuilder`

- Types matched: `0` / `220`

## Assembly: `Unity.ProBuilder.AddOns.Editor`

- Types matched: `0` / `2`

## Assembly: `Unity.ProBuilder.AssetIdRemapUtility`

- Types matched: `0` / `28`

## Assembly: `Unity.ProBuilder.Csg`

- Types matched: `11` / `16`

### `UnityEngine.ProBuilder.Csg.CSG`

- Public: `False`
- IsAbstract: `True`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- `static UnityEngine.ProBuilder.Csg.Model Intersect(UnityEngine.GameObject lhs, UnityEngine.GameObject rhs)`
- `static UnityEngine.ProBuilder.Csg.Model Perform(UnityEngine.ProBuilder.Csg.CSG+BooleanOp op, UnityEngine.GameObject lhs, UnityEngine.GameObject rhs)`
- `static UnityEngine.ProBuilder.Csg.Model Subtract(UnityEngine.GameObject lhs, UnityEngine.GameObject rhs)`
- `static UnityEngine.ProBuilder.Csg.Model Union(UnityEngine.GameObject lhs, UnityEngine.GameObject rhs)`

#### Fields
- (none)

### `UnityEngine.ProBuilder.Csg.CSG+BooleanOp`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Enum`

#### Methods
- (none)

#### Fields
- `static UnityEngine.ProBuilder.Csg.CSG+BooleanOp Intersection`
- `static UnityEngine.ProBuilder.Csg.CSG+BooleanOp Subtraction`
- `static UnityEngine.ProBuilder.Csg.CSG+BooleanOp Union`
- `System.Int32 value__`

### `UnityEngine.ProBuilder.Csg.Model`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- (none)

#### Fields
- (none)

### `UnityEngine.ProBuilder.Csg.Model+<>c__DisplayClass15_0`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- (none)

#### Fields
- `UnityEngine.Transform transform`

### `UnityEngine.ProBuilder.Csg.Node`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- `System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] AllPolygons()`
- `System.Void Build(System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] list)`
- `System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] ClipPolygons(System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] list)`
- `System.Void ClipTo(UnityEngine.ProBuilder.Csg.Node other)`
- `UnityEngine.ProBuilder.Csg.Node Clone()`
- `static UnityEngine.ProBuilder.Csg.Node Intersect(UnityEngine.ProBuilder.Csg.Node a1, UnityEngine.ProBuilder.Csg.Node b1)`
- `System.Void Invert()`
- `static UnityEngine.ProBuilder.Csg.Node Subtract(UnityEngine.ProBuilder.Csg.Node a1, UnityEngine.ProBuilder.Csg.Node b1)`
- `static UnityEngine.ProBuilder.Csg.Node Union(UnityEngine.ProBuilder.Csg.Node a1, UnityEngine.ProBuilder.Csg.Node b1)`

#### Fields
- `UnityEngine.ProBuilder.Csg.Node back`
- `UnityEngine.ProBuilder.Csg.Node front`
- `UnityEngine.ProBuilder.Csg.Plane plane`
- `System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] polygons`

### `UnityEngine.ProBuilder.Csg.Plane`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- `System.Void Flip()`
- `System.Void SplitPolygon(UnityEngine.ProBuilder.Csg.Polygon polygon, System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] coplanarFront, System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] coplanarBack, System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] front, System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Polygon, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] back)`
- `System.String ToString()`
- `System.Boolean Valid()`

#### Fields
- `UnityEngine.Vector3 normal`
- `System.Single w`

### `UnityEngine.ProBuilder.Csg.Plane+EPolygonType`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Enum`

#### Methods
- (none)

#### Fields
- `static UnityEngine.ProBuilder.Csg.Plane+EPolygonType Back`
- `static UnityEngine.ProBuilder.Csg.Plane+EPolygonType Coplanar`
- `static UnityEngine.ProBuilder.Csg.Plane+EPolygonType Front`
- `static UnityEngine.ProBuilder.Csg.Plane+EPolygonType Spanning`
- `System.Int32 value__`

### `UnityEngine.ProBuilder.Csg.Polygon`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- `System.Void Flip()`
- `System.String ToString()`

#### Fields
- `UnityEngine.Material material`
- `UnityEngine.ProBuilder.Csg.Plane plane`
- `System.Collections.Generic.List`1[[UnityEngine.ProBuilder.Csg.Vertex, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] vertices`

### `UnityEngine.ProBuilder.Csg.Vertex`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.ValueType`

#### Methods
- `System.Void Flip()`
- `System.Boolean HasArrays(UnityEngine.ProBuilder.Csg.VertexAttributes attribute)`

#### Fields
- (none)

### `UnityEngine.ProBuilder.Csg.VertexAttributes`

- Public: `False`
- IsAbstract: `False`
- IsSealed: `True`
- BaseType: `System.Enum`

#### Methods
- (none)

#### Fields
- `static UnityEngine.ProBuilder.Csg.VertexAttributes All`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Color`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Lightmap`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Normal`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Position`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Tangent`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Texture0`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Texture1`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Texture2`
- `static UnityEngine.ProBuilder.Csg.VertexAttributes Texture3`
- `System.Int32 value__`

### `UnityEngine.ProBuilder.Csg.VertexUtility`

- Public: `False`
- IsAbstract: `True`
- IsSealed: `True`
- BaseType: `System.Object`

#### Methods
- `static System.Void GetArrays(System.Collections.Generic.IList`1[[UnityEngine.ProBuilder.Csg.Vertex, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] vertices, UnityEngine.Vector3[]& position, UnityEngine.Color[]& color, UnityEngine.Vector2[]& uv0, UnityEngine.Vector3[]& normal, UnityEngine.Vector4[]& tangent, UnityEngine.Vector2[]& uv2, System.Collections.Generic.List`1[[UnityEngine.Vector4, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]& uv3, System.Collections.Generic.List`1[[UnityEngine.Vector4, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]& uv4)`
- `static System.Void GetArrays(System.Collections.Generic.IList`1[[UnityEngine.ProBuilder.Csg.Vertex, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] vertices, UnityEngine.Vector3[]& position, UnityEngine.Color[]& color, UnityEngine.Vector2[]& uv0, UnityEngine.Vector3[]& normal, UnityEngine.Vector4[]& tangent, UnityEngine.Vector2[]& uv2, System.Collections.Generic.List`1[[UnityEngine.Vector4, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]& uv3, System.Collections.Generic.List`1[[UnityEngine.Vector4, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]& uv4, UnityEngine.ProBuilder.Csg.VertexAttributes attributes)`
- `static UnityEngine.ProBuilder.Csg.Vertex[] GetVertices(UnityEngine.Mesh mesh)`
- `static UnityEngine.ProBuilder.Csg.Vertex Mix(UnityEngine.ProBuilder.Csg.Vertex x, UnityEngine.ProBuilder.Csg.Vertex y, System.Single weight)`
- `static System.Void SetMesh(UnityEngine.Mesh mesh, System.Collections.Generic.IList`1[[UnityEngine.ProBuilder.Csg.Vertex, Unity.ProBuilder.Csg, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] vertices)`
- `static UnityEngine.ProBuilder.Csg.Vertex TransformVertex(UnityEngine.Transform transform, UnityEngine.ProBuilder.Csg.Vertex vertex)`

#### Fields
- (none)

## Assembly: `Unity.ProBuilder.Editor`

- Types matched: `0` / `344`

## Assembly: `Unity.ProBuilder.KdTree`

- Types matched: `0` / `22`

## Assembly: `Unity.ProBuilder.Poly2Tri`

- Types matched: `0` / `38`

## Assembly: `Unity.ProBuilder.Stl`

- Types matched: `0` / `12`

