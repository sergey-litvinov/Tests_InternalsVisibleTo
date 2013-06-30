Tests_InternalsVisibleTo
========================

Sample project that show security problem with InternalsVisibleTo attribute.

.Net attribute InternalsVisibleTo allows for specified assemblies to use internal methods\classes of this assembly. 
The problem is that we can dynamically create new assembly, copy PublicKey to this new assembly, and make new operations to internal members.

For example, if we have a class with internal virtual method, in case if we use this issue, we can create a new class that will override this internal method, and then we can modify behaviour of that class
