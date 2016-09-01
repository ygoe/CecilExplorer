## Mono.Cecil object model explorer

This tool shows the object hierarchy that Mono.Cecil reads from a CIL (.NET) assembly. Use it to learn about how compiled source code is seen by Cecil and how you can interpret and manipulate assemblies.

The members list contains all public properties and fields as well as parameterless methods returning a value. Anything IEnumerable lists the collection items instead of any members.

The Type column shows the interface type and, if different, the instance runtime type of the value.

[Project website](http://unclassified.software/apps/cecilexplorer)

## Quick start

Drag an assembly file to inspect onto the window or press Ctrl+O to select a file. Press F1 to learn more about the keyboard interaction and colours used.

## License

Copyright (c) 2016, Yves Goergen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Contains [SharpTreeView](https://github.com/icsharpcode/SharpDevelop/tree/master/src/Libraries/SharpTreeView):<br>
Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team

Uses [Mono.Cecil](https://github.com/jbevain/cecil/):<br>
Copyright (c) 2008 - 2015 Jb Evain<br>
Copyright (c) 2008 - 2011 Novell, Inc.
