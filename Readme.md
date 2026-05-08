# Sharped Beacon Color Calculator

**A high-performance tool for finding the mathematically perfect glass combination for any beacon color in Minecraft.**

This tool doesn't use "good enough" approximations. In v2, we've moved to a highly optimized hybrid architecture combining **KD-Trees** for real-time spatial searching and **Look-Up Tables (LUT)** to provide the absolute best match for all 16.7 million RGB colors instantly.

## Features
* Uses **Oklab** color space for perceptual color matching (Delta E 76), which is far superior to standard RGB Euclidean distance.
* **Hybrid Pipeline:** Uses lightweight KD-Trees for 2 to 5 glass layers (finding matches in `O(log N)`), and massive precomputed LUTs for 6 to 8 layers (`O(1)`) (can be configured to works differenty).
* **Blazing Fast:** The KD-Tree algorithm is so optimized that it can perform 100,000 lookups for 5 layers of glass in just ~3 seconds.
* **Zstandard (ZST) Compression:** The entire database of trees and LUTs is now packed into a single, highly efficient `beacon_caches.zst` file, replacing the old GZIP format.
* Smart data packing: Color sequences are bit-shifted into `short`, `int`, or `long` depending on length to minimize RAM usage.

## How it Works

1. Generates all possible glass combinations following Minecraft's blending formula: `(current + next) / 2`.
2. Identifies all unique colors that a beacon can actually display for a given depth.
3. For up to 5 layers, it builds a spatial **KD-Tree** based on L, A, B coordinates.
4. For deeper layers (6-8) where KD-Trees aren't fast enough for bulk operations, the generator precomputes a 16.7M elements LUT by multithreading queries against the KD-Tree.
5. Everything is serialized and compressed into a single `.zst` archive.

## Usage

### The pre-built `beacon_caches.zst` can be found in Releases
You don't have to compute anything. Just download the pre-built cache from the Releases page, load it into your project, and start querying colors immediately.

### Precomputing the Cache (Do it yourself)
If you want to generate the cache yourself, change the number of layers, or implement a different approach — you are free to do so. Run (change) the built-in generator pipeline:

```csharp
using BeaconColorCalculator.Generator;

// This will generate KD-Trees (up to 5 layers) and LUTs (up to 8 layers),
// and save everything into a compressed ZST archive.
Pipeline.GenerateCaches("your_output_directory");
```
### Looking up a color
Load the archive and use either the KD-Trees or LUTs to get your glass sequence:

```csharp
using BeaconColorCalculator.Core.Cache;
using BeaconColorCalculator.Core.Models;

// 1. Load the compressed cache (contains all trees and LUTs)
var storage = BeaconStorage.Load("beacon_caches.zst");

// 2. Define your target color (e.g., Lilac)
var target = OklabColor.FromRgb(new RgbColor(200, 162, 200));

// 3. Lookup using a 5-layer KD-Tree
var kdTreeNodes = storage.KdTreesInt[5];
var tree = new OklabKdTree<int>(kdTreeNodes, kdTreeNodes.Length);

var sequence = tree.FindNearest(target);

// Output: Red -> Blue -> White...
Console.WriteLine($"Found sequence: {sequence}");
```