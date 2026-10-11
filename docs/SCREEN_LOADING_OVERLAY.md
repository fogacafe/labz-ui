# ScreenLoadingOverlay (experimental)

Add the overlay to the same container as the content it should cover, **after** the content. Bind it to a screen-scoped `IScreenLoadingManager` and bring it to front. It appears while any operation token is active, displays the latest active operation message and count, and uses a WinForms marquee progress bar.

This is a first visual proof of concept, not yet a theme-aware Labz spinner. It uses an opaque background rather than true alpha blending. It blocks mouse interactions with covered controls, but **does not guarantee keyboard focus or accessibility blocking**. Host navigation and cancellation remain the host's responsibility. `Changed` may arrive on a worker thread; UI changes are marshaled when the handle exists. An event arriving before handle creation can be missed until the next state refresh. Manual Windows UI QA is required before production use.
