#[cfg(any(target_os = "macos", target_os = "windows"))]
use tauri::Manager;
#[cfg(target_os = "windows")]
use window_vibrancy::{apply_acrylic, apply_blur};
#[cfg(target_os = "macos")]
use window_vibrancy::{apply_vibrancy, NSVisualEffectMaterial, NSVisualEffectState};

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

            #[cfg(any(target_os = "macos", target_os = "windows"))]
            if let Some(window) = app.get_webview_window("main") {
                #[cfg(target_os = "macos")]
                if let Err(error) = apply_vibrancy(
                    &window,
                    NSVisualEffectMaterial::UnderWindowBackground,
                    Some(NSVisualEffectState::FollowsWindowActiveState),
                    Some(14.0),
                ) {
                    eprintln!("Unable to apply macOS window vibrancy: {error}");
                }

                #[cfg(target_os = "windows")]
                {
                    // Windows requires a native backdrop; CSS backdrop-filter only blurs
                    // content behind an element and cannot blur the desktop window.
                    let result = apply_acrylic(&window, Some((18, 18, 18, 125)))
                        .or_else(|_| apply_blur(&window, Some((18, 18, 18, 125))));
                    if let Err(error) = result {
                        eprintln!("Unable to apply Windows window backdrop: {error}");
                    }
                }
            }

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
