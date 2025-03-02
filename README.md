# Luau-CSharp
C# bindings for Luau, built in mind for unity, IL2CPP and .Net
These bindings are not that fast, but it gets the job done.

# Set Up
To set up Luau-CSharp, you need the binaries. You could compile these yourself via the [cmake project](https://github.com/KinexDev/Luau-CSharp-Build) or you could use the precompiled binaries present in the repo.
after you imported all the scripts and binaries to your project you need to initalise the VM, The VM manages & abstracts the luau state from you, these are not fully high-level bindings though, you still need to manage some of it by yourself.
to initalise the VM, it's the following

```cs
vm = new VM(Console.WriteLine);
```
