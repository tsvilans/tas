# tas 1.2
A personal toolkit for research and exploration. Uses RhinoCommon for geometric types and provides an interface through Grasshopper components.

# License

```
Copyright 2016-2020 Tom Svilans

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

Current version: 1.1

# Modules

## Core

The Core module contains Types, Extension Methods, and Utility classes. Types include a Polyline that uses planes instead of points as vertices (oriented polyline), a Pose which is a position and orientation with a fitness value, and a couple of Network implementations for facilitating graph-based workflows. Extension Methods provide extra methods for RhinoCommon types (Polyline, Vector3d, Point3d, Plane, Mesh, Brep, Curve, etc.). Utility classes provide some new functionality that doesn't quite fit into the extension methods.

The GH extended version provides some wrapper classes and components for the Grasshopper plug-in.

## Lam

The Lam module contains classes and types developed for my PhD research into free-form timber structures. The Glulam class allows the definition of straight, single-curved, and double-curved glulam beams; generating their geometry; analyzing curvature and bending limits; calculating required lamella composition; and more. The Workpiece class and Feature class extend the Glulam model into the fabrication of joints.

Once again, the GH extension for this provides new Grasshopper components for modelling and analyzing free-form glulam members. 

## Machine

The Machine module contains classes and types for toolpath generation and CNC machining. Many of these are self-rolled imitations of common toolpath strategies (area clearance, pocket, flowline, etc.) and are mostly experimental. 

Again, the GH extension provides some new Grasshopper components for generating and modifying toolpaths. There is some initial support for post-processors for some specific machines such as the Haas TM-3 3-axis vertical mills and the CMS Antares 5-axis wood processing centre.

## Fun

The Fun module contains implementations of algorithms and other experimental work that is highly volatile and unstable. This is meant as a place to store developing ideas and experiments that don't yet have a home in the other modules. These include basic implementations of Simulated Annealing, K-Means clustering, and Metropolis-Hastings; as well as a tentative next version of the Glulam model.

# Acknowledgements

This library has been developed for and during an EU-funded PhD project at the [Centre for IT and Architecture (CITA)](https://kadk.dk/en/CITA) in Copenhagen, Denmark, as part of the [Innochain](http://innochain.net/) training network. 

More information about the project can be found [here](http://innochain.net/esr2-integrating-material-performance/).

This project has received funding from the European Union’s Horizon 2020 research and innovation programme under the Marie Sklodowska-Curie grant agreement No 642877.

This project uses the following libraries:
- [Clipper](http://www.angusj.com/delphi/clipper.php)
- [Carve](https://github.com/VTREEM/Carve) through the [Carverino](https://github.com/tsvilans/carverino) wrapper.
- [Triangle .NET](https://archive.codeplex.com/?p=triangle)

# Contact

http://tomsvilans.com
