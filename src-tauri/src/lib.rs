#[cfg(any(target_os = "macos", target_os = "windows"))]
use tauri::Manager;
#[cfg(target_os = "windows")]
use window_vibrancy::{apply_acrylic, apply_blur};
#[cfg(target_os = "macos")]
use window_vibrancy::{apply_vibrancy, clear_vibrancy, NSVisualEffectMaterial, NSVisualEffectState};

const WINDOW_LABEL: &str = "main";

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }

            // The window starts hidden (see tauri.conf.json "visible": false).
            // Revealing it only after the first page load forces a fresh WKWebView
            // composite pass, which avoids the known macOS/transparent-window
            // blank-until-reload issue. A fallback timer guarantees the window is
            // never left invisible when the page signal never arrives.
            #[cfg(any(target_os = "macos", target_os = "windows"))]
            if let Some(window) = app.get_webview_window(WINDOW_LABEL) {
                apply_window_backdrop_impl(&window, true);

                let fallback = window.clone();
                std::thread::spawn(move || {
                    std::thread::sleep(std::time::Duration::from_secs(8));
                    let _ = fallback.show();
                });
            }

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            apply_window_backdrop,
            host_content_ready
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

/// Invoked by the sample host once the first page has fully loaded. Shows the
/// hidden window at that point so the initial composite renders the UI.
#[tauri::command]
fn host_content_ready(app: tauri::AppHandle) -> Result<(), String> {
    if let Some(window) = app.get_webview_window(WINDOW_LABEL) {
        let _ = window.show();
        let _ = window.set_focus();
    }
    Ok(())
}

/// Re-applies the native window backdrop to match the resolved page theme.
///
/// This lives on the Tauri host side and is invoked by the sample app whenever
/// `ThemeProvider` flips `data-theme` on `<html>`. The component library stays
/// host-agnostic and never talks to this command directly.
#[tauri::command]
fn apply_window_backdrop(app: tauri::AppHandle, theme: String) -> Result<(), String> {
    #[cfg(any(target_os = "macos", target_os = "windows"))]
    {
        let Some(window) = app.get_webview_window(WINDOW_LABEL) else {
            return Ok(());
        };
        apply_window_backdrop_impl(&window, theme.eq_ignore_ascii_case("dark"));
    }
    Ok(())
}

#[cfg(any(target_os = "macos", target_os = "windows"))]
fn apply_window_backdrop_impl(window: &tauri::WebviewWindow, dark: bool) {
    #[cfg(target_os = "macos")]
    {
        let _ = dark; // The material adapts to the window appearance already.
        // window-vibrancy appends a fresh NSVisualEffectView on every call, so
        // clear the previous one before re-applying to avoid stacking views.
        let _ = clear_vibrancy(window);
        if let Err(error) = apply_vibrancy(
            window,
            NSVisualEffectMaterial::UnderWindowBackground,
            Some(NSVisualEffectState::FollowsWindowActiveState),
            Some(14.0),
        ) {
            eprintln!("Unable to apply macOS window vibrancy: {error}");
        }
    }

    #[cfg(target_os = "windows")]
    {
        // Windows requires a native backdrop; CSS backdrop-filter only blurs
        // content behind an element and cannot blur the desktop window.
        let tint = if dark {
            (18, 18, 18, 125)
        } else {
            (245, 247, 250, 150)
        };
        let result = apply_acrylic(window, Some(tint)).or_else(|_| apply_blur(window, Some(tint)));
        if let Err(error) = result {
            eprintln!("Unable to apply Windows window backdrop: {error}");
        }
    }
}
