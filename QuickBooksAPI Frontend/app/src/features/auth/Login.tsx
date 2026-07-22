import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { AlertCircle, Loader2, Eye, EyeOff, Lock, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { StandalonePageShell } from '@/components/layout/StandalonePageShell';

export function Login() {
  const navigate = useNavigate();
  const { login, isLoading } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!email || !password) {
      setError('Please enter both email and password');
      return;
    }

    const success = await login(email, password);
    if (success) {
      navigate('/', { replace: true });
    }
  };

  return (
    <StandalonePageShell
      headerRight={
        <a className="text-sm text-muted-foreground hover:text-primary transition-colors" href="#">
          Help
        </a>
      }
    >
      <div className="w-full max-w-[440px]">
        <Card className="shadow-md p-8 gap-6">
          <div className="text-center space-y-1">
            <h1 className="text-2xl font-semibold text-foreground">Sign In</h1>
            <p className="text-sm text-muted-foreground">Access your business dashboard</p>
          </div>

          <CardContent className="p-0">
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              {error && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              <div className="space-y-1.5">
                <Label htmlFor="email">Email or User ID</Label>
                <Input
                  id="email"
                  type="text"
                  placeholder="Enter your email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={isLoading}
                  className="h-12"
                  required
                />
              </div>

              <div className="space-y-1.5">
                <div className="flex justify-between items-center">
                  <Label htmlFor="password">Password</Label>
                  <a className="text-xs font-semibold text-tertiary hover:underline" href="#">
                    Forgot password?
                  </a>
                </div>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Enter your password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    disabled={isLoading}
                    className="h-12 pr-10"
                    required
                  />
                  <button
                    type="button"
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                    onClick={() => setShowPassword((v) => !v)}
                    tabIndex={-1}
                  >
                    {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </button>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <input
                  id="remember"
                  type="checkbox"
                  className="h-4 w-4 rounded border-input text-primary focus:ring-primary"
                />
                <Label htmlFor="remember" className="font-normal">Remember me</Label>
              </div>

              <Button type="submit" size="lg" className="w-full h-12 rounded-full mt-1" disabled={isLoading}>
                {isLoading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Signing in...
                  </>
                ) : (
                  'Sign In'
                )}
              </Button>
            </form>

            <div className="relative flex items-center gap-3 py-4">
              <div className="flex-grow h-px bg-border" />
              <span className="text-xs font-semibold text-muted-foreground">OR</span>
              <div className="flex-grow h-px bg-border" />
            </div>

            <div className="flex flex-col gap-4">
              <Button
                type="button"
                variant="outline"
                className="w-full h-12 rounded-full"
                onClick={() => toast.info('Google sign-in is not available yet')}
              >
                <svg className="h-5 w-5" viewBox="0 0 48 48" aria-hidden="true">
                  <path fill="#FFC107" d="M43.6 20.5H42V20H24v8h11.3c-1.6 4.6-6 8-11.3 8-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.9 1.1 8 3l5.7-5.7C34.6 6 29.6 4 24 4 12.9 4 4 12.9 4 24s8.9 20 20 20 20-8.9 20-20c0-1.2-.1-2.4-.3-3.5z"/>
                  <path fill="#FF3D00" d="M6.3 14.7l6.6 4.8C14.6 15.9 18.9 13 24 13c3.1 0 5.9 1.1 8 3l5.7-5.7C34.6 6 29.6 4 24 4c-7.5 0-14 4.2-17.3 10.4z"/>
                  <path fill="#4CAF50" d="M24 44c5.5 0 10.4-1.9 14.2-5.1l-6.5-5.5c-2.1 1.5-4.8 2.4-7.7 2.4-5.3 0-9.7-3.4-11.3-8.1l-6.6 5C9.9 39.6 16.4 44 24 44z"/>
                  <path fill="#1976D2" d="M43.6 20.5H42V20H24v8h11.3c-.8 2.3-2.2 4.2-4.1 5.6l6.5 5.5C41.4 36 44 30.6 44 24c0-1.2-.1-2.4-.4-3.5z"/>
                </svg>
                Sign in with Google
              </Button>

              <div className="text-center pt-1">
                <span className="text-sm text-muted-foreground">New to QuickBooks?</span>{' '}
                <Link to="/register" className="text-sm font-semibold text-tertiary hover:underline">
                  Sign up
                </Link>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="mt-6 flex items-center justify-center gap-4 opacity-60">
          <div className="flex items-center gap-1.5">
            <Lock className="h-4 w-4" />
            <span className="text-xs font-semibold">Secure SSL Encryption</span>
          </div>
          <div className="w-1 h-1 rounded-full bg-muted-foreground" />
          <div className="flex items-center gap-1.5">
            <ShieldCheck className="h-4 w-4" />
            <span className="text-xs font-semibold">Privacy Protected</span>
          </div>
        </div>
      </div>
    </StandalonePageShell>
  );
}
