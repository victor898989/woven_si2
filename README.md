# Getting started with PicoGK

PicoGK ("peacock") is a compact and robust geometry kernel for Computational Engineering.

You can find general information on [PicoGK.org](https://picogk.org) and the the [PicoGK repository on GitHub](https://leap71.com/PicoGK).

This repository contains example code, which showcases various aspects of PicoGK.

You can download this repository's source code to get an instant PicoGK-ready environment to play around with.

For more information, see the [PicoGK documentation on PicoGK.org](https://picogk.org/doc/)

# MATERIAL [ Plasma Flow Boundary: High Heat Flux q" ]
 ──────────────────────────────────────────────────────────
  ░░░░░░░ Sacrificial Layer (1-2 mm) V(x,y,z) Phase-Front
  ══════════════════════════════════════════════════════════
  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
  ▒▒▒▒▒▒▒▒ High-Emittance Ceramic Fiber Matrix ▒▒▒▒▒▒▒▒▒▒▒
  ▒▒▒▒▒▒▒▒      Structural Core Layer (4 cm)   ▒▒▒▒▒▒▒▒▒▒▒
  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
 ──────────────────────────────────────────────────────────
                     [ Inner Airframe ]

# Running PicoGK

Download this example repository, open in VisualStudio Code, and run the code `Program.cs`.

The examples are organized into subfolders, according to the their category.

# OTHER
Geometric considerations and viewing factors The formulas provided for concentric cylinders and finite areas highlight the importance of calculating viewing factors ($F_{ij}$). These represent the fraction of radiation leaving surface $i$ that directly strikes surface $j$. Concentric Cylinders For concentric cylinders of different finite lengths, the view factors ($F_{12}$) involve the $K$ parameters, where $K_m^2 = A_m F_{m m'}$. These relationships allow the determination of heat transfer between complex internal geometries. Insensitivity to Cavity Shape A crucial finding in thermal design is that for isothermal cavities where the ratio of aperture area to total internal area ($A_h/A_c$) is greater than 0.5, the apparent emittance becomes largely insensitive to the specific cavity shape. Computational Engineering Context The provided C# code uses LEAP 71 ShapeKernel (based on PicoGK) to generate lattice structures. While the theoretical documentation focuses on radiative analysis, this code represents the geometric implementation of a computational model. Grid generation: Functions such as latFromLine, latFromBeam, and latFromGrid allow the discretization of complex 3D shapes. Engineering application: In the context of the provided ECSS standards, these grid generation tools could be used to model the thermally active internal surfaces of a cavity or to design structural supports that maintain the isothermal conditions required for high-precision sensors or radiative heat exchangers.

# Monolitic microfibers using lattice/quasicrystals from piko gk
The microstructure of the UHTC composite no longer relies on adhesively bonded layers. The combination of ZrB₂-SiC-TaSi₂ forms continuous chemical bonds with the carbon-carbon chassis through a linear thermal gradient quasicrystalline microfiber network. This avoids thermal expansion coefficient (TEC) discontinuities, eliminating delamination induced by mechanical stresses during and after periods of maximum combustion and re-entry.

[ Ambient Air / Plasma Interface ]
      │
      ▼ (Oxygen Diffusion)
┌───────────┐  High Viscosity Liquid Flow  ┌───────────┐
│           │ ───────────►       ◄──────── │           │
│  Matrix   │  (SiO2 - B2O3 Borosilicate)  │  Matrix   │
│           │                              │           │
│           │       ▲              ▲       │           │
└───────────┴───────┼──────────────┼───────┴───────────┘
                    │              │
         [ m-ZrO2 Volume Expansion Compressive Stress ]
         [         Pins the Crack Tip Spatially       ]
