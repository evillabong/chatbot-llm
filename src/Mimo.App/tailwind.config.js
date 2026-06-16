/** @type {import('tailwindcss').Config} */
module.exports = {
    // Escanea las apps Y la librería de UI Mimo.Ui (donde viven las clases reales).
    content: [
        "./App.razor",
        "./wwwroot/**/*.html",
        "./Layout/**/*.razor",
        "./Pages/**/*.razor",
        "../Mimo.Ui/**/*.razor"
    ],
    darkMode: 'class',
    theme: {
        extend: {
            colors: {
                primary: { "50": "#eff6ff", "100": "#dbeafe", "200": "#bfdbfe", "300": "#93c5fd", "400": "#60a5fa", "500": "#3b82f6", "600": "#2563eb", "700": "#1d4ed8", "800": "#1e40af", "900": "#1e3a8a", "950": "#172554" }
            }
        },
        fontFamily: {
            'body': ['Inter', 'ui-sans-serif', 'system-ui', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
            'sans': ['Inter', 'ui-sans-serif', 'system-ui', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
            'mono': ['ui-monospace', 'SFMono-Regular', 'Cascadia Mono', 'Courier New', 'monospace']
        }
    }
}
