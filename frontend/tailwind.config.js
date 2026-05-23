/** @type {import(''tailwindcss'').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        crimson: '#8B0000',
        gold: '#C9A84C',
        jade: '#2D6A4F',
        ink: '#1A1A2E',
        parchment: '#F5F0E8',
        copper: '#B87333',
        mist: '#E8E0D0',
      },
      fontFamily: {
        heading: ['Cinzel', 'serif'],
        body: ['Lato', 'sans-serif'],
      },
      boxShadow: {
        glow: '0 18px 40px rgba(26, 26, 46, 0.18)',
        gold: '0 0 0 1px rgba(201, 168, 76, 0.35), 0 18px 36px rgba(26, 26, 46, 0.14)',
      },
      backgroundImage: {
        hero: 'radial-gradient(circle at top, rgba(201,168,76,0.18), transparent 38%), linear-gradient(135deg, rgba(139,0,0,0.96), rgba(26,26,46,0.96))',
        parchment: 'radial-gradient(circle at top left, rgba(201,168,76,0.14), transparent 32%), linear-gradient(180deg, rgba(245,240,232,0.96), rgba(232,224,208,0.92))',
      },
    },
  },
  plugins: [],
}

