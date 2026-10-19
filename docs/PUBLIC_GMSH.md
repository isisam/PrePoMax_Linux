# Gmsh source boundary

Matej Borovinšek's unpublished Gmsh fork is not distributed here. The vendored Windows Gmsh SDK (including patched headers and all library versions), QMorph library and their historical copies are excluded. Do not restore them from an old PrePoMax package. The public PrePoMax integration source is licensed with PrePoMax; it is not the Gmsh fork implementation.

The selected rcm/l3b2 Linux lineage did not merge L1g native interop. GmshCommon.Stub builds an API-shaped assembly and **every native operation throws NotSupportedException**. This is the documented meshing stub; it produces no mesh. Existing-mesh import, layup/3D fibre-display source and the shared core remain available. Windows branches retain their original C++/CLI project references but cannot build that backend without an independently reviewed public-SDK adaptation. Full Windows solution/release parity is not claimed after exclusion.

Public source only: https://gmsh.info/src/gmsh-4.13.0-source.tgz , SHA256 c85f056ee549a433e814a61c385c97952bbfe514b442b999f6149fffb1e54f64. The public archive was downloaded and hashed during preparation. To build a standard public Gmsh library with existing development tools:

```bash
tar -xzf gmsh-4.13.0-source.tgz
cmake -S gmsh-4.13.0-source -B gmsh-public-build -DENABLE_BUILD_LIB=ON -DENABLE_OPENMP=ON
cmake --build gmsh-public-build -j4
```

Retain Gmsh LICENSE/CREDITS. This builds public Gmsh, not a drop-in parity backend for this application. A future implementation must bind the documented public gmshc.h ABI, enforce version/memory/error contracts and refuse unsupported prescribed-node, QMorph and fork-specific sweep operations. Do not claim fork parity or enable the backend merely because the shared library loads.
