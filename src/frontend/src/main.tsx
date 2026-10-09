import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ThemeProvider } from './theme/ThemeContext';
import { FeedbackProvider } from './components/Feedback';
import { NavigationProtectionProvider } from './components/NavigationProtection';
import { App } from './App';
import './styles.css';
import { startPerformanceMonitoring } from './performance/monitoring';
startPerformanceMonitoring();

const router = createBrowserRouter([{ path: '*', element: <ThemeProvider><AuthProvider><FeedbackProvider><NavigationProtectionProvider><App /></NavigationProtectionProvider></FeedbackProvider></AuthProvider></ThemeProvider> }]);
createRoot(document.getElementById('root')!).render(<StrictMode><RouterProvider router={router} /></StrictMode>);
