# blazor-lazyloading

This repository shows up two ways of lazy loading for blazor WASM projects.
- BlazorLazyLoadingMicrosoft:
  This is the official Microsoft way with the Router and its property AdditionalAssemblies and hooking in with an OnNavigateAsync method to load assemblies that are not already present.
- BlazorLazyLoadingAlternate:
  This is an adpotion of the Microsoft code of the Router and some services for routes, page types and assemblies. 
