# BMW Drive Recorder Processor — Bug Fix Log

## Summary of Changes

All fixes applied to the codebase (except issue #5 — only first `.ts` file processed per directory, deferred by user request).

---

## `src/Processor.cs`

### Fix 1 — Removed unused `using` directives
**Issue #10** — `System.Collections.Generic`, `System.Linq`, `System.Runtime.CompilerServices`, `System.Text`, and `System.Threading.Tasks` were all imported but never used.  
**Fix:** Removed all five unused `using` statements, leaving only `System`, `System.Diagnostics`, `System.Globalization`, and `System.Text.Json`.

---

### Fix 2 — Leading semicolon in ffmpeg `complexFilters` string (non-split/scale/flip path)
**Issue #3** — When neither `SplitVideo`, `ScaleVideo`, nor `FlipRearCam` were enabled, the `else` branch constructed the `complexFilters` string starting with `";"`, which is invalid ffmpeg filter syntax and would cause ffmpeg to fail.  
```csharp
// Before (BUG):
complexFilters = ";[0]" + param.InterpolationAlgo + "[interpolated]";

// After (FIXED):
complexFilters = "[0]" + param.InterpolationAlgo + "[interpolated]";
```

---

### Fix 3 — Null-dereference on `JsonSerializer.Deserialize` result
**Issue #4** — `Metadata[]` returned from `JsonSerializer.Deserialize<Metadata[]>()` could be `null`, but was used without a null check, causing a potential `NullReferenceException`.  
**Fix:** Changed type to `Metadata[]?` and added a combined `null || Length <= 0` guard before use.

---

### Fix 4 — File streams not disposed on exception (`createSubtitles`)
**Issue #7** — `FileStream` and `StreamReader` for reading `Metadata.json`, and `FileStream` + `StreamWriter` for writing the `.ass` subtitle file, were not wrapped in `using` blocks. An exception mid-method would leak those handles.  
**Fix:** Wrapped the metadata read in a nested `using` block; the output stream and writer now use C# `using` declarations, guaranteeing disposal even on exception. Removed the now-redundant explicit `.Close()` calls.

---

### Fix 5 — ffmpeg not found produces no diagnostic
**Issue #6** — If `ffmpeg` was not installed or not on `PATH`, `Process.Start()` would throw a `Win32Exception` with native error code 2 (`ERROR_FILE_NOT_FOUND`), and the app would crash with an unhandled exception.  
**Fix:** Wrapped `runFfmpeg` in a `try/catch` for `Win32Exception` (error code 2). On catch, sets `log` to a descriptive message:  
> `"ffmpeg not found. Please ensure ffmpeg is installed and available on your system PATH."`  
and returns `false` so the caller can write the failure to `log.log`.

---

### Fix 6 — Stray semicolon inside `if` block in `writeEntries`
**Issue #9** — A spurious `;` appeared immediately after the `{` opening the `if (param.MphMode)` block, creating a no-op empty statement.  
```csharp
// Before (BUG):
if (param.MphMode)
{;
	speedStr = ...

// After (FIXED):
if (param.MphMode)
{
	speedStr = ...
```

---

## `src/DriveRecorderProcessorForm.cs`

### Fix 7 — Unused `using System.ComponentModel.DataAnnotations`
**Issue #11** — `System.ComponentModel.DataAnnotations` was imported but not referenced anywhere in the file.  
**Fix:** Removed the `using` directive.

---

### Fix 8 — Success/failure display logic was inverted
**Issue #1** — The `allSuccess` conditional was backwards: on success it wrote the error log file; on failure it showed the "Processing successful" message box.  
```csharp
// Before (BUG):
if (allSuccess)          // ← writes log on SUCCESS
	File.WriteAllText("log.log", log);
else
	MessageBox.Show("Processing successful");  // ← shown on FAILURE

// After (FIXED):
if (!allSuccess)         // ← writes log on FAILURE
{
	File.WriteAllText("log.log", log);
	MessageBox.Show("Processing failed. See log.log for details.", "Error", ...);
}
else
{
	MessageBox.Show("Processing successful", "Done", ...);
}
```

---

### Fix 9 — `FrontAndRear` camera selection restored wrong radio button on load
**Issue #2** — When loading saved settings, `CameraSelection.FrontAndRear` incorrectly checked `FrontCamRadio` instead of `RearCamRadio`.  
```csharp
// Before (BUG):
case CameraSelection.FrontAndRear:
	FrontCamRadio.Checked = true;  // wrong!

// After (FIXED):
case CameraSelection.FrontAndRear:
	RearCamRadio.Checked = true;
```

---

### Fix 10 — UI froze during video processing (synchronous on UI thread)
**Issue #8** — `processor.process()` calls `ffmpeg` synchronously via `Process.WaitForExit()` inside `StartBtn_Click`, blocking the Windows message loop for the entire processing duration. The form became unresponsive with no visual feedback.  
**Fix:** Changed `StartBtn_Click` to `async void` and offloaded all `Processor.process()` calls to a background thread via `await Task.Run(...)`. The UI thread remains responsive; `StartBtn` is disabled for the duration and re-enabled when complete.

---

### Fix 11 — Settings file streams not disposed on exception (`DriveRecorderConverterForm_Load`)
**Issue #7 (secondary)** — The `FileStream` / `StreamReader` pair reading `settings.json` on form load, and the `Deserialize` result, were not guarded for null.  
**Fix:** Wrapped the file read in nested `using` blocks; added a `null` check on the deserialized `ProcessorParameter` with an early `return` if null.

---

## Build Result
✅ Build successful — 0 errors, 0 warnings.
