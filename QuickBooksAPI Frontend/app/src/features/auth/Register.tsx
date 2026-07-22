import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  AlertCircle,
  Loader2,
  CheckCircle,
  Eye,
  EyeOff,
  User,
  Mail,
  Lock,
  ShieldCheck,
  CloudCheck,
} from 'lucide-react';
import { toast } from 'sonner';
import { StandalonePageShell } from '@/components/layout/StandalonePageShell';

const SPECIAL_CHAR_REGEX = /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/;

export function Register() {
  const navigate = useNavigate();
  const { signUp, isLoading } = useAuth();
  const [formData, setFormData] = useState({ firstName: '', lastName: '', username: '', email: '', password: '', confirmPassword: '' });
  const [agreedToTerms, setAgreedToTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState('');

  const validatePassword = (password: string): string[] => {
    const errors: string[] = [];
    if (password.length < 8) errors.push('At least 8 characters');
    if (password.length > 100) errors.push('No more than 100 characters');
    if (!/[A-Z]/.test(password)) errors.push('One uppercase letter');
    if (!/[a-z]/.test(password)) errors.push('One lowercase letter');
    if (!/[0-9]/.test(password)) errors.push('One number');
    if (!SPECIAL_CHAR_REGEX.test(password)) errors.push('One special character');
    return errors;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!formData.firstName || !formData.lastName || !formData.username || !formData.email || !formData.password) {
      setError('Please fill in all fields');
      return;
    }
    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }
    const passwordErrors = validatePassword(formData.password);
    if (passwordErrors.length > 0) {
      setError('Please fix password requirements');
      return;
    }
    if (!agreedToTerms) {
      setError('Please agree to the Terms of Service and Privacy Policy');
      return;
    }

    const success = await signUp({
      firstName: formData.firstName,
      lastName: formData.lastName,
      username: formData.username,
      email: formData.email,
      password: formData.password,
    });

    if (success) {
      navigate('/login', { replace: true });
    }
  };

  const passwordReqs = [
    { test: formData.password.length >= 8 && formData.password.length <= 100, text: '8-100 characters' },
    { test: /[A-Z]/.test(formData.password), text: 'One uppercase letter' },
    { test: /[a-z]/.test(formData.password), text: 'One lowercase letter' },
    { test: /[0-9]/.test(formData.password), text: 'One number' },
    { test: SPECIAL_CHAR_REGEX.test(formData.password), text: 'One special character' },
  ];

  return (
    <StandalonePageShell
      headerRight={
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">Already have an account?</span>
          <Link to="/login" className="text-sm font-bold text-primary hover:underline">
            Sign in
          </Link>
        </div>
      }
    >
      <div className="w-full max-w-lg bg-card rounded-xl shadow-sm border border-border overflow-hidden">
        <div className="p-6 md:p-8 space-y-6">
          <div className="text-center space-y-1">
            <h1 className="text-2xl font-semibold text-foreground">Create your account</h1>
            <p className="text-sm text-muted-foreground">Join the community of accounting professionals</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="firstName">First Name</Label>
                <Input id="firstName" name="firstName" placeholder="e.g. Jane" value={formData.firstName} onChange={handleChange} disabled={isLoading} required />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="lastName">Last Name</Label>
                <Input id="lastName" name="lastName" placeholder="e.g. Doe" value={formData.lastName} onChange={handleChange} disabled={isLoading} required />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="username">Username</Label>
              <div className="relative">
                <User className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input id="username" name="username" placeholder="Choose a unique username" value={formData.username} onChange={handleChange} disabled={isLoading} className="pl-10" required />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="email">Email Address</Label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input id="email" name="email" type="email" placeholder="jane.doe@company.com" value={formData.email} onChange={handleChange} disabled={isLoading} className="pl-10" required />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Min. 8 characters"
                  value={formData.password}
                  onChange={handleChange}
                  disabled={isLoading}
                  className="pl-10 pr-10"
                  required
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-primary"
                  onClick={() => setShowPassword((v) => !v)}
                  tabIndex={-1}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {formData.password && (
                <div className="text-sm space-y-1 pt-1">
                  <p className="text-muted-foreground">Password requirements:</p>
                  <ul className="space-y-1">
                    {passwordReqs.map((req, i) => (
                      <li key={i} className={`flex items-center gap-1 ${req.test ? 'text-success' : 'text-destructive'}`}>
                        {req.test ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}{req.text}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Re-enter your password"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  disabled={isLoading}
                  className="pl-10 pr-10"
                  required
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-primary"
                  onClick={() => setShowConfirmPassword((v) => !v)}
                  tabIndex={-1}
                >
                  {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {formData.confirmPassword && formData.password !== formData.confirmPassword && (
                <p className="text-sm text-destructive">Passwords do not match</p>
              )}
            </div>

            <div className="flex items-start gap-2 py-1">
              <input
                id="terms"
                type="checkbox"
                checked={agreedToTerms}
                onChange={(e) => setAgreedToTerms(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-input text-primary focus:ring-primary"
              />
              <Label htmlFor="terms" className="font-normal text-muted-foreground leading-tight">
                I agree to the <a className="text-tertiary hover:underline" href="#">Terms of Service</a> and{' '}
                <a className="text-tertiary hover:underline" href="#">Privacy Policy</a>.
              </Label>
            </div>

            <Button type="submit" size="lg" className="w-full h-12 rounded-full" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating account...
                </>
              ) : (
                'Create Account'
              )}
            </Button>

            <div className="relative flex items-center py-2">
              <div className="flex-grow border-t border-border" />
              <span className="mx-4 text-xs font-semibold text-muted-foreground uppercase tracking-widest">or</span>
              <div className="flex-grow border-t border-border" />
            </div>

            <Button
              type="button"
              variant="outline"
              className="w-full h-12 rounded-full"
              onClick={() => toast.info('Google sign-up is not available yet')}
            >
              <svg className="h-5 w-5" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
                <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
                <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
                <path d="M12 5.38c1.62 0 3.06.56 4.21 1.66l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
              </svg>
              Sign up with Google
            </Button>
          </form>

          <div className="flex flex-wrap items-center justify-center gap-3 pt-2">
            <div className="flex items-center gap-1.5 px-3 py-1 rounded-full border border-border bg-muted">
              <Lock className="h-4 w-4 text-primary" />
              <span className="text-xs text-muted-foreground">Secure SSL</span>
            </div>
            <div className="flex items-center gap-1.5 px-3 py-1 rounded-full border border-border bg-muted">
              <ShieldCheck className="h-4 w-4 text-primary" />
              <span className="text-xs text-muted-foreground">Privacy Protected</span>
            </div>
            <div className="flex items-center gap-1.5 px-3 py-1 rounded-full border border-border bg-muted">
              <CloudCheck className="h-4 w-4 text-primary" />
              <span className="text-xs text-muted-foreground">Intuit Verified</span>
            </div>
          </div>
        </div>

        <div className="bg-accent p-4 border-t border-border text-center">
          <p className="text-sm text-muted-foreground">
            Need help? <a className="text-tertiary font-bold hover:underline" href="#">Contact Support</a> or visit our{' '}
            <a className="text-tertiary font-bold hover:underline" href="#">Help Center</a>.
          </p>
        </div>
      </div>
    </StandalonePageShell>
  );
}
