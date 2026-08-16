/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        background: 'oklch(0.13 0.02 300)',
        foreground: 'oklch(0.96 0.005 300)',
        card: {
          DEFAULT: 'oklch(0.17 0.025 300)',
          foreground: 'oklch(0.96 0.005 300)',
        },
        popover: {
          DEFAULT: 'oklch(0.16 0.025 300)',
          foreground: 'oklch(0.96 0.005 300)',
        },
        primary: {
          DEFAULT: 'oklch(0.58 0.25 295)',
          foreground: 'oklch(0.98 0.005 300)',
        },
        secondary: {
          DEFAULT: 'oklch(0.22 0.03 300)',
          foreground: 'oklch(0.96 0.005 300)',
        },
        muted: {
          DEFAULT: 'oklch(0.22 0.03 300)',
          foreground: 'oklch(0.65 0.02 300)',
        },
        accent: {
          DEFAULT: 'oklch(0.68 0.2 300)',
          foreground: 'oklch(0.13 0.02 300)',
        },
        destructive: {
          DEFAULT: 'oklch(0.65 0.22 25)',
        },
        border: 'oklch(1 0 0 / 10%)',
        input: 'oklch(1 0 0 / 12%)',
        ring: 'oklch(0.58 0.25 295)',
        success: 'oklch(0.72 0.19 150)',
        gold: 'oklch(0.8 0.16 85)',
        sidebar: {
          DEFAULT: 'oklch(0.11 0.02 300)',
          foreground: 'oklch(0.96 0.005 300)',
          primary: 'oklch(0.58 0.25 295)',
          'primary-foreground': 'oklch(0.98 0.005 300)',
          accent: 'oklch(0.2 0.03 300)',
          'accent-foreground': 'oklch(0.96 0.005 300)',
          border: 'oklch(1 0 0 / 8%)',
          ring: 'oklch(0.58 0.25 295)',
        },
      },
      borderRadius: {
        lg: '0.75rem',
        md: 'calc(0.75rem * 0.8)',
        sm: 'calc(0.75rem * 0.6)',
      },
      keyframes: {
        'play-pulse': {
          '0%, 100%': {
            boxShadow: '0 0 24px oklch(0.58 0.25 295 / 55%), 0 0 60px oklch(0.58 0.25 295 / 25%)',
          },
          '50%': {
            boxShadow: '0 0 40px oklch(0.58 0.25 295 / 80%), 0 0 90px oklch(0.58 0.25 295 / 40%)',
          },
        },
      },
      animation: {
        'play-pulse': 'play-pulse 2.4s ease-in-out infinite',
      },
    },
  },
  plugins: [],
}
