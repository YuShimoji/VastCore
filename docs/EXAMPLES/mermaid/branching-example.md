# Mermaid 分岐サンプル

以下は flowchart で分岐を表現する最小例です。`tools/mermaid/mermaid-preview.html` に貼り付けて、IDs に `B,C` を指定すると該当ノードがハイライトされます。

```mermaid
flowchart TD
  A[Start] --> B{Condition?}
  B -- Yes --> C[Path 1]
  B -- No  --> D[Path 2]
  C --> E[Merge]
  D --> E[Merge]
  E --> F[End]
```

## Tips
- ノードIDは角括弧やテキストとは別に、左辺の識別子（`A`, `B`, `C` など）です。
- 複数ハイライトする場合はカンマ区切りで指定してください（例: `A,E,F`）。
