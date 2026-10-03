// Entry point: Vite loads index.html -> src/main.tsx -> <App />.
// createRoot mounts React inside the <div id="root"> element.
// StrictMode double-runs effects in dev to surface bugs early (no effect in production).
// Theme lives in App.tsx (light/dark toggle), so main stays a plain mount.
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
