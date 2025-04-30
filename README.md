# VoxelEngine with destruction for Unity

Voxel destruction engine using Unity’s Job System and Burst Compiler.

## Background

This project began as an experiment while I was still learning programming. As a result, parts of the code and design decisions might not be ideal (see Known Limitations/ Things to Improve).
Over the years, I’ve occasionally revisited the project to refactor or clean things up, but for the most part, it has just been sitting around. I've decided to share it now in case it's helpful or interesting.

## Demo

[![Watch the Demo](https://img.youtube.com/vi/ZRyKRpgqTM4/0.jpg)](https://www.youtube.com/watch?v=ZRyKRpgqTM4)


## Installation

### Option 1: Unity Package Manager
1. Open Unity Package Manager
2. Click **+ → Add package from Git URL**
3. Paste: `https://github.com/Kugelhaufen/VoxelEngine.git`

### Importing the Demo Scene
Once the package is installed:
1. In Unity's Package Manager, select the package.
2. Go to the Samples tab.
3. Click Import.
4. A folder will be created at: Assets/Samples/VoxelEngine/<VersionNumber>


### Option 2: Manual Copy
1. Copy the `VoxelEngine/` folder into your Unity `Assets/`
2. Install listed dependencies manually (see `package.json`)
(This is recommended if you're planning to extend or modify the code.)

> ⚠️ Currently, this package only supports the **Built-in Render Pipeline**. No Shaders for URP or HDRP have been written yet.

---


## Quick Start

### Sample Game:
- Import Sample Game via Package Manager
- Supports touch & mouse input.
- Scroll wheel switches weapons.
- Change voxel model by editing `VoxelObj -> VoxelMap` in the scene.
-`VoxelObjAmountManager` limits the number of active voxel objects for performance.
  - Automatically deletes small debris objects when over threshold.
  - Connected component extraction events auto-register with this manager.

### Creating Voxel Objects (Edit-Time)
1. Create an empty GameObject.
2. Add the `VoxelObj` MonoBehaviour.
> This defines an empty voxel object. It won't do anything until you attach other components or interact with it via script.

### Add Destruction & Physics
To enable "physics-based" separation via connected components:
1. Add the `ConnectedComponentPhysics` MonoBehaviour.
2. Assign the required **ScriptableObject algorithms** via the Inspector:
- **Extractor:**
  - `Default Parallel Interrupted Extractor` – Keeps the largest chunk static.
  - `Default Parallel Interrupted Gravity Extractor` - Keeps the lowest chunk static. (e.g. prevents floating voxels)
- **Voxel Replace Updater:**
  - `Standard` – Fast but may cause mesh flickering (Z-fighting) during the update 
  - `Anti-Flicker` – Prevents z-fighting by temporarily offsetting new voxels before mesh update (slower).
> Note if you **installed via Package Manager**: Unity does not automatically show available ScriptableObjects in the Inspector if they are in the packages folder. To fix this, manually drag them from the Packages/ folder into your Assets/ directory so you can assign them.

### Auto-Initialization at Runtime
Add the `VoxelObjStartInitializer` component to automatically generate a voxel object at scene start.

**Properties:**
- Immediate Update: Skips background threading, generates mesh immediately (can cause frame freeze).
- Physics: If enabled, voxel object is a Rigidbody instead of static.
- Destroy After Init: Removes the initializer component after use.
- Voxel Map: The serialized voxel structure.
- Chunk Material: Material using the voxel shader.

> Toggle "Show Preview Gizmos" to see a red voxel map preview in the scene editor.

## Runtime Behavior & Object Structure
### Spawning Voxel Objects
To create voxel objects during runtime, use:
```C#
VoxelObj.InstantiateVoxelObj();
```
> This is faster than using Instantiate(VoxelObj) directly


## VoxelObj Internals

At runtime:
- A `VoxelObjHolder` is created, containing:
  - The original voxel object
  - A `Chunks` child object with actual mesh chunks
> To move the object in-world, move the `VoxelObjHolder`, not the `VoxelObj` itself.

### Update Methods
- `FullVoxelObjUpdate()` – Async update of all chunks.
- `FullVoxelObjUpdateImmediate()` – Immediate blocking update.
- `VoxelObjUpdate()` – Async update of chunks that are in `NextUpdateChunkBatch`.
- `VoxelObjUpdateImmediate()` – Immediate blocking update of chunks that are in `NextUpdateChunkBatch`.

### Job System & Callbacks

This engine uses Unity's Job System and Burst Compiler for multithreaded performance.

To manage jobs that run over multiple frames, the `JobCallbackManager`:
- Monitors job completion each frame.
- Invokes callbacks when done.
- Invokes cancel delegates and/or disposes registered IDisposable instances when it gets destroyed (e.g. on Scene change)

> ⚠️ Don't manually destroy the JobCallbackManager GameObject unless you're unloading the scene or quitting.

### Potential Memory Leaks

Due to heavy use of Unity’s Native Collections and multi-frame job handling, memory leaks can occur if callbacks or disposables aren't properly managed. I've fixed known issues during development, but always be cautious when extending the engine or writing custom scripts.
When using the `JobCallbackManager` always make sure to not forget about any NativeCollections: Dispose them in your callback methods and register them with the JobCallbackManager or dispose them in your cancel-callback methods.

## Editor Tools (Menu: `Tools > VoxelEngine`)
1. Hollow VoxelObj – Turns a voxel map into a hollow shell.
2. MeshCollider Tool – Adds mesh colliders to GameObjects and all children (helps with voxelization).
3. Voxelize – Converts a GameObject into a voxel structure (`SerializedVoxelMap`).
4. VoxelMap Resizer – Scales voxel maps using nearest-neighbor resampling.

## Further Exploration
To understand how to:
- Update voxel objects via code
- Hook into callbacks
- Create runtime voxel structures
- Handle destruction events
> Check the scripts used in the Sample Game. Most key features are demonstrated there.


## Known Limitations/ Things to Improve

- The algorithms used for meshing, connected component labeling, etc., are mostly custom and not fully optimized. There are better and more established algos. Also there’s too much threading in places where single-threaded execution might have been more efficient.
- Voxel data is stored in a flattened array. While functional, it's not optimal for performance with larger objects. The `SerializedVoxelMap` serializes and compresses this array. Something like Sparse Voxel Octree is likely better.
- The engine only supports individual (small to medium-sized) voxel objects — not full maps or streamed/segmented worlds. It’s not designed for infinite terrain or large environments right now.
- Voxel objects above a certain size (around 200×200×200) may cause noticeable slowdowns, especially during connected component extraction and updates.
- There's no support for established voxel formats (like MagicaVoxel .vox) — only internal formats and the built-in voxelizer.
- Only the Built-in Render Pipeline is supported. No shaders exist yet for URP or HDRP.
- Transparency and other advanced material features are missing.
> This list is not complete

## Maintenance
This project is shared as-is. I’m not actively maintaining it right now, and I’m not sure if I will in the future.
