import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const HostGridPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#fcf4ef', 100: '#f7e5db', 200: '#efcbbb', 300: '#e7aa91',
      400: '#d08165', 500: '#b45338', 600: '#9b442e', 700: '#843921',
      800: '#692e21', 900: '#52271e', 950: '#321812',
    },
    colorScheme: {
      light: { primary: { color: '{primary.600}', contrastColor: '#ffffff', hoverColor: '{primary.700}', activeColor: '{primary.800}' } },
      dark: { primary: { color: '{primary.300}', contrastColor: '#30251f', hoverColor: '{primary.200}', activeColor: '{primary.400}' } },
    },
  },
});
