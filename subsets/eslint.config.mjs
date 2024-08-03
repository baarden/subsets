import path from 'node:path';
import { fileURLToPath } from 'node:url';
import js from '@eslint/js';
import react from 'eslint-plugin-react';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import typescriptPlugin from '@typescript-eslint/eslint-plugin';
import importPlugin from 'eslint-plugin-import';
import typescriptParser from '@typescript-eslint/parser';
import nextPlugin from '@next/eslint-plugin-next';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default [
  {
    files: ['**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      sourceType: 'module',
      parser: typescriptParser,
      parserOptions: {
        project: [
	  './tsconfig.json',
	  './apps/next/tsconfig.json',
	  './packages/app/tsconfig.json',
	],
        tsconfigRootDir: __dirname,
      },
      globals: {
        window: 'readonly',
        document: 'readonly',
        localStorage: 'readonly',
        console: 'readonly',
      },
    },
    plugins: {
      '@typescript-eslint': typescriptPlugin,
      react,
      'jsx-a11y': jsxA11y,
      import: importPlugin,
      '@next/next': nextPlugin,
    },
    rules: {
      ...js.configs.recommended.rules,
      ...react.configs.recommended.rules,
      ...typescriptPlugin.configs.recommended.rules,
      ...jsxA11y.configs.recommended.rules,
      ...importPlugin.configs.recommended.rules,
      ...importPlugin.configs.typescript.rules,
      ...nextPlugin.configs.recommended.rules,
      'react/jsx-key': 'error',
      '@next/next/no-html-link-for-pages': ['error', 'apps/next/pages'],
      'import/no-unresolved': 'error',
    },
    settings: {
      react: {
        version: 'detect',
      },
      'import/resolver': {
        typescript: {
          alwaysTryTypes: true,
          project: [
	    './tsconfig.json',
	    './apps/next/tsconfig.js',
	    './packages/app/tsconfig.js',
	  ],
        },
      },
    },
    ignores: [
      '**/node_modules',
      '**/dist',
      '**/types',
      'apps/next/out',
      'apps/next/.next',
      'apps/next/.tamagui',
    ],
  },
];
