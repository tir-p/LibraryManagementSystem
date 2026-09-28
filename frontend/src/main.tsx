// Entry point: Vite loads index.html -> src/main.tsx -> <App />.
// createRoot mounts React inside the <div id="root"> element.
// StrictMode double-runs effects in dev to surface bugs early (no effect in production).
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
