# QuickSort Implementation Plan

## Solution Structure

```
QuickSorting.sln
├── QuickSorting/                  (Class Library — .NET 8)
│   ├── QuickSorting.csproj
│   ├── ISort.cs
│   └── QuickSort.cs
└── QuickSorting.Tests/            (xUnit Test Project — .NET 8)
    ├── QuickSorting.Tests.csproj
    └── QuickSortTests.cs
```

## Files

### `ISort.cs`
Define a generic sorting interface:
```csharp
public interface ISort
{
    int[] Sort(int[] array);
}
```

### `QuickSort.cs`
Implement `ISort` using the in-place quicksort algorithm:
- Public `Sort(int[] array)` — entry point, calls recursive helper
- Private `QuickSort(int[] array, int low, int high)` — recursive partitioning
- Private `Partition(int[] array, int low, int high)` — Lomuto partition scheme, returns pivot index
- Pivot selection: last element

### `QuickSortTests.cs`
Unit test cases using xUnit:

| Test | Input | Expected |
|------|-------|----------|
| Already sorted | `[1, 2, 3, 4, 5]` | `[1, 2, 3, 4, 5]` |
| Reverse sorted | `[5, 4, 3, 2, 1]` | `[1, 2, 3, 4, 5]` |
| Random order | `[3, 6, 8, 10, 1, 2, 1]` | `[1, 1, 2, 3, 6, 8, 10]` |
| Duplicates | `[4, 4, 4, 4]` | `[4, 4, 4, 4]` |
| Single element | `[42]` | `[42]` |
| Empty array | `[]` | `[]` |
| Negative numbers | `[-3, 1, -1, 0, 2]` | `[-3, -1, 0, 1, 2]` |

## Project File Details

### `QuickSorting.csproj`
- SDK: `Microsoft.NET.Sdk`
- TargetFramework: `net8.0`
- Nullable: enabled
- ImplicitUsings: enabled

### `QuickSorting.Tests.csproj`
- SDK: `Microsoft.NET.Sdk`
- TargetFramework: `net8.0`
- Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- Project reference: `QuickSorting`

## Implementation Steps

1. Create solution file `QuickSorting.sln`
2. Create `QuickSorting` class library project
3. Add `ISort.cs` with the interface
4. Add `QuickSort.cs` implementing the interface
5. Create `QuickSorting.Tests` xUnit project
6. Add project reference from Tests to QuickSorting
7. Add `QuickSortTests.cs` with all test cases
8. Verify the solution opens and builds in Visual Studio 2022
