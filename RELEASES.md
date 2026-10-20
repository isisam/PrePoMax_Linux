# Releases

Use https://github.com/isisam/PrePoMax_Linux/releases . The selected Linux teaching checkpoint is 0.4.0, tagged v0.4.0-teaching.1. Original RC1f/RC1g5 Windows binary ZIPs are not publishable: they contain the excluded modified Gmsh SDK and legacy model history paths. Do not upload them or the old full source archive. The Windows source feature lines remain windows-rc/develop.

Linux binary asset: PrePoMax-Linux-0.4.0-teaching.tar.gz. This newly created repack omits the research solver and debug symbol files. Its application/native code comes from the original L3 checkpoint and was not rebuilt. The asset remains HOLD while embedded CodeView developer paths are unresolved. Source publication and binary release privacy are assessed separately in the owner's external audit.

The corresponding public source archive contains the content-filtered history's selected source tree and all retained notices. Public Gmsh meshing is a documented refusal stub. No private Gmsh source is required for the Linux checkpoint. See docs/PUBLIC_GMSH.md and docs/BUILD_PUBLIC.md.
