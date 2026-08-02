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
        background: 'oklch(var(--background) / <alpha-value>)',
        foreground: 'oklch(var(--foreground) / <alpha-value>)',
        card: {
          DEFAULT: 'oklch(var(--card) / <alpha-value>)',
          foreground: 'oklch(var(--card-foreground) / <alpha-value>)',
        },
        popover: {
          DEFAULT: 'oklch(var(--popover) / <alpha-value>)',
          foreground: 'oklch(var(--popover-foreground) / <alpha-value>)',
        },
        primary: {
          DEFAULT: 'oklch(var(--primary) / <alpha-value>)',
          foreground: 'oklch(var(--primary-foreground) / <alpha-value>)',
        },
        secondary: {
          DEFAULT: 'oklch(var(--secondary) / <alpha-value>)',
          foreground: 'oklch(var(--secondary-foreground) / <alpha-value>)',
        },
        muted: {
          DEFAULT: 'oklch(var(--muted) / <alpha-value>)',
          foreground: 'oklch(var(--muted-foreground) / <alpha-value>)',
        },
        accent: {
          DEFAULT: 'oklch(var(--accent) / <alpha-value>)',
          foreground: 'oklch(var(--accent-foreground) / <alpha-value>)',
        },
        destructive: {
          DEFAULT: 'oklch(var(--destructive) / <alpha-value>)',
        },
        border: 'oklch(1 0 0 / 10%)',
        input: 'oklch(1 0 0 / 12%)',
        ring: 'oklch(var(--ring) / <alpha-value>)',
        success: 'oklch(0.72 0.19 150 / <alpha-value>)',
        gold: 'oklch(0.8 0.16 85 / <alpha-value>)',
        sidebar: {
          DEFAULT: 'oklch(var(--sidebar) / <alpha-value>)',
          foreground: 'oklch(var(--sidebar-foreground) / <alpha-value>)',
          primary: 'oklch(var(--sidebar-primary) / <alpha-value>)',
          'primary-foreground': 'oklch(var(--sidebar-primary-foreground) / <alpha-value>)',
          accent: 'oklch(var(--sidebar-accent) / <alpha-value>)',
          'accent-foreground': 'oklch(var(--sidebar-accent-foreground) / <alpha-value>)',
          border: 'oklch(1 0 0 / 8%)',
          ring: 'oklch(var(--ring) / <alpha-value>)',
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
            boxShadow: '0 0 24px oklch(var(--primary) / 55%), 0 0 60px oklch(var(--primary) / 25%)',
          },
          '50%': {
            boxShadow: '0 0 40px oklch(var(--primary) / 80%), 0 0 90px oklch(var(--primary) / 40%)',
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
