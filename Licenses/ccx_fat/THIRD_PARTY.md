# Components of ccx_fat.exe v1.1b-win64 and their licences

| component | version | licence | text in this folder |
|---|---|---|---|
| CalculiX CrunchiX (ccx) | 2.21 (Debian/Ubuntu source package calculix-ccx 2.21-1) | GPL-2.0 | `COPYING.GPL-2.0_CalculiX.txt`, `CalculiX_debian_copyright.txt` |
| FATCZM fatigue cohesive UMAT + cycle-jump hook | v1.1rc (fatczm.f unchanged since v1.1) | GPL-2.0 (same as CalculiX) | `COPYING.GPL-2.0_CalculiX.txt` |
| SPOOLES (with the multi-threaded MT part) | 2.2 (spooles 2.2-14.1build2) | public domain | `SPOOLES_copyright.txt` |
| arpack-ng | 3.9.1 (arpack 3.9.1-1.1build2) | BSD-3-Clause | `ARPACK-NG_COPYING.txt` |
| reference BLAS and LAPACK (netlib) | 3.12.0 (lapack 3.12.0-3build1.1) | modified BSD | `LAPACK_BLAS_LICENSE.txt` |
| MinGW-w64 C runtime, winpthreads | 11.0.1 (mingw-w64 11.0.1-3build1) | ZPL-2.1, MIT-style, public domain and others (per file) | `MinGW-w64_runtime_and_winpthreads_copyright.txt` |
| GCC runtime libraries libgfortran, libgomp, libgcc | GCC 13.2.0 (gcc-mingw-w64 13.2.0-6ubuntu1+26.1) | GPL-3.0 with the GCC Runtime Library Exception 3.1 | `GPL-3.0_GCC_runtime.txt`, `GCC_Runtime_Library_Exception-3.1.txt`, `GCC_runtime_libgfortran_libgomp_libgcc_copyright.txt` |
| libquadmath (GCC quad-precision math library, used by libgfortran) | GCC 13.2.0 (same toolchain) | GNU LGPL 2.1 or later; Copyright (C) Free Software Foundation, Inc. and the other authors named in its source files | `LGPL-2.1_libquadmath.txt` |

All libraries are linked statically into `ccx_fat.exe`.

- The complete corresponding source of `ccx_fat.exe` -- CalculiX with FATCZM, SPOOLES, ARPACK, LAPACK and the build
  scripts -- is in `ccx_fat_win64_v1.1b_source.zip` (this RC package: `Source\ccx_fat\`).
  v1.1b is v1.1 rebuilt from the same source with relative file names (no build-machine paths in the executable).
- **libquadmath (LGPL-2.1, section 6):** the same zip contains the complete source of libquadmath as used
  (`libquadmath/gcc-13.2.0/`, from the GCC 13.2.0 release), the Ubuntu packaging that built the toolchain's copy, and a
  tested recipe (`windows_build/build_libquadmath.sh`, then `build_ccx.sh` with `QUADMATH_DIR`) to build libquadmath
  from source, possibly modified, and relink `ccx_fat.exe` with it. See `README_SOURCE.md`, section "libquadmath". The
  start-up banner of `ccx_fat.exe` names libquadmath and this licence.
- libgfortran, libgomp and libgcc are used under the GCC Runtime Library Exception 3.1, which allows distributing the
  combined executable under terms of our choice (here GPL-2.0, from CalculiX); their sources are part of GCC 13.2.0
  (https://gcc.gnu.org; Ubuntu source packages `gcc-13` and `gcc-mingw-w64` 26.1).
- The MinGW-w64 runtime and winpthreads are under permissive licences; their sources are in the Ubuntu source package
  `mingw-w64` 11.0.1-3build1 and at https://www.mingw-w64.org.

In this RC package, the solver banner's standalone source\ and LICENSES\ locations correspond to Source\ccx_fat\ and Licenses\ccx_fat\LGPL-2.1_libquadmath.txt, respectively.
