# Sharped Beacon Color Calculator 

**A high-performance tool for finding the mathematically perfect glass combination for any beacon color in Minecraft.**

This tool doesn't use "good enough" approximations. 
It uses a precomputed 64MB Look-Up Table to provide the absolute best match for all 16.7 million RGB colors in O(1).

## Features
* Uses **Oklab** color space for perceptual color matching (Delta E 76), which is far superior to standard RGB Euclidean distance.
* Uses **SIMD (AVX-512 / AVX2)** and multi-threading to process trillion of color comparisons in an hour.
* Provides O(1) lookup speed (approx. 5-10 nanoseconds per color).
* The entire 16.7M color database is stored in a 64MB binary file, which is GZIP-compressed to ~14MB for distribution.

## How it Works

1.  Generates all possible 17.8 million glass combinations (up to 6 layers) following Minecraft's blending formula: `(current + next) / 2`.
2.  Identifies ~3.19 million unique colors that a beacon can actually display.
3.  For every possible RGB color ($256^3$), it finds the closest match from the unique colors using vectorized Oklab distance.
4.  Each result is packed into a 32-bit integer (3 bits for length, 24 bits for 6 colors) for minimal memory footprint.

## Usage

## The pre-built `beacon_cache.bin.gz` can be found in Releases

### Precomputing the Cache
Run the generator once to create the `beacon_cache.bin.gz` file. On a mid-CPU like i5-10400, this took 150 minutes.

### Looking up a color
```csharp
// Load the compressed cache
var cache = new BeaconCache("beacon_cache.bin.gz");

// O(1) Lookup
var target = new RgbColor(200, 162, 200); // Lilac
var sequence = cache.GetSequence(target);

Console.WriteLine($"Layers: {sequence.Length}");
foreach (var colorId in sequence.ToArray()) 
{
    Console.WriteLine(MinecraftBlender.ColorNames[colorId]);
}