# tas
A .NET personal toolkit for research and exploration. Uses RhinoCommon for geometric types and provides an interface through Grasshopper components.

# License

```
Copyright 2016-2018 Tom Svilans

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

To re-iterate the above, this code is at risk of random refactoring, modification, overhaul, and general breakage at any given moment in time. It will liberally introduce new dependencies with little documentation, or perform unexpectedly. If it works for you, save it and back it up. This is a personal toolkit and code diary for on-going research, exploration, learning, and other such nonsense, and as such is a pretty raw reflection of my state-of-mind. Use at own risk. 

# Modules

The toolkit is divided into some separate modules, some of which heavily depend on one-another.

## Core

This contains class extensions and utility functions which I find myself using over and over again. It defines some new types and extends loads of other ones, and also provides interfaces for converting between some types and others. The GH extended version provides some wrapper classes and components for the Grasshopper plug-in.

## Lam

An ecology of classes and types developed for my PhD research into free-form timber and its production. Documentation may one day appear. The GH extension for this provides loads of new Grasshopper components for modelling and analyzing free-form glulam members. Not all of them work as expected; some of them might be broken, I haven't checked recently.

## Machine

A bunch of classes and types to assist with toolpath generation and processing. These are self-rolled imitations of common toolpath strategies (area clearance, pocket, flowline, etc.). These were developed out of necessity, for lack of available toolpath generation tools at the time. They have been a great learning tool and can no doubt be improved. Again, the GH extension provides some new Grasshopper components for generating and modifying toolpaths. There is some initial support for post-processors for some specific machines. This development is on-going and is an in-progress refactor of existing stuff that was used to translate paths into machine code. 

## Fun

This is where some tangential experimentation and learning happens. This module contains implementations of various algorithms that I have come across and have used for certain things. This is a way to document them, keep them around, and try to flesh them out as more generic tools for future use. These include basic pseudo-implementations of Simulated Annealing, K-Means clustering, and Metropolis-Hastings.

# Acknowledgements

This library has been developed for and during an EU-funded PhD project at the [Centre for IT and Architecture (CITA)](https://kadk.dk/en/CITA) in Copenhagen, Denmark, as part of the [Innochain](http://innochain.net/) training network. 

More information about the project can be found [here](http://innochain.net/esr2-integrating-material-performance/).

This project has received funding from the European Union’s Horizon 2020 research and innovation programme under the Marie Sklodowska-Curie grant agreement No 642877.

# Contact

tsvi@kadk.dk

http://tomsvilans.com
