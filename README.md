# Cartographic Printing / Картографічний друк

Windows x64 desktop application for preparing cartographic raster images for printing.

## Current implemented functions

- Open PNG, JPG/JPEG, BMP and TIFF raster images locally.
- Preview the selected map.
- A4, A3, A2, A1, A0, 610 mm roll and custom page dimensions.
- Portrait and landscape paper setup.
- Fit-to-page printing.
- Exact physical print scaling based on image DPI and source/target map scale.
- Configurable margins.
- Standard Windows printer selection and printing.
- Local processing: opened map files are not uploaded by the application.

## Microsoft Store package

The repository includes a GitHub Actions workflow that builds:

`Cartographic_Printing_Setup_x64.msi`

The MSI is self-contained: users do not need to install Python or the .NET runtime.

### Important: Microsoft Store MSI/EXE signing

Microsoft requires an MSI/EXE Store submission and its PE files to be Authenticode-signed with a trusted code-signing certificate. Self-signed certificates are not accepted for public Store submission.

Configure these repository secrets before creating the Store release:

- `WINDOWS_SIGNING_PFX_BASE64`
- `WINDOWS_SIGNING_PFX_PASSWORD`

Then open **Actions → Build Store MSI → Run workflow** and use version `1.0.0`.

If signing secrets are not configured, the workflow still produces an unsigned MSI artifact for testing, but intentionally does **not** create a GitHub Release for Microsoft Store submission.

After a successful signed release `v1.0.0`, the Partner Center package URL is:

`https://github.com/mapgeoint-ship-it/cartographic-printing/releases/download/v1.0.0/Cartographic_Printing_Setup_x64.msi`

Partner Center settings:

- Architecture: `x64`
- App type: `MSI`
- Languages: Ukrainian and English
- Silent install: Microsoft Store uses `/qn` automatically for MSI packages.

## Privacy

The application processes selected map files locally on the user's Windows computer.
