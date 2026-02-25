import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle, Loader2, Building2, CheckCircle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

export function Register() {
  const navigate = useNavigate();
  const { signUp, isLoading } = useAuth();
  const [formData, setFormData] = useState({ firstName: '', lastName: '', username: '', email: '', password: '', confirmPassword: '' });
  const [error, setError] = useState('');

  const validatePassword = (password: string): string[] => {
    const errors: string[] = [];
    if (password.length < 8) errors.push('At least 8 characters');
    if (password.length > 100) errors.push('No more than 100 characters');
    if (!/[A-Z]/.test(password)) errors.push('One uppercase letter');
    if (!/[a-z]/.test(password)) errors.push('One lowercase letter');
    if (!/[0-9]/.test(password)) errors.push('One number');
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) errors.push('One special character');
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
    { test: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(formData.password), text: 'One special character' },
  ];

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-background to-muted p-4">
      <div className="w-full max-w-md">
        <div className="flex justify-center mb-8">
          <div className="flex items-center gap-2">
            <div className="bg-primary p-2 rounded-lg">
              <Building2 className="h-8 w-8 text-primary-foreground" />
            </div>
            <span className="text-2xl font-bold">QuickBooks Connect</span>
          </div>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Create Account</CardTitle>
            <CardDescription>Sign up to connect your QuickBooks Online account</CardDescription>
          </CardHeader>
          <form onSubmit={handleSubmit}>
            <CardContent className="space-y-4">
              {error && <Alert variant="destructive"><AlertCircle className="h-4 w-4" /><AlertDescription>{error}</AlertDescription></Alert>}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2"><Label htmlFor="firstName">First Name</Label><Input id="firstName" name="firstName" placeholder="John" value={formData.firstName} onChange={handleChange} disabled={isLoading} required /></div>
                <div className="space-y-2"><Label htmlFor="lastName">Last Name</Label><Input id="lastName" name="lastName" placeholder="Doe" value={formData.lastName} onChange={handleChange} disabled={isLoading} required /></div>
              </div>
              <div className="space-y-2 mt-6"><Label htmlFor="username">Username</Label><Input id="username" name="username" placeholder="johndoe" value={formData.username} onChange={handleChange} disabled={isLoading} required /></div>
              <div className="space-y-2 mt-6"><Label htmlFor="email">Email</Label><Input id="email" name="email" type="email" placeholder="name@company.com" value={formData.email} onChange={handleChange} disabled={isLoading} required /></div>
              <div className="space-y-2 mt-6">
                <Label htmlFor="password">Password</Label>
                <Input id="password" name="password" type="password" placeholder="••••••••" value={formData.password} onChange={handleChange} disabled={isLoading} required />
                {formData.password && (
                  <div className="text-sm space-y-1">
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
              <div className="space-y-2 mt-6">
                <Label htmlFor="confirmPassword">Confirm Password</Label>
                <Input id="confirmPassword" name="confirmPassword" type="password" placeholder="••••••••" value={formData.confirmPassword} onChange={handleChange} disabled={isLoading} required />
                {formData.confirmPassword && formData.password !== formData.confirmPassword && <p className="text-sm text-destructive">Passwords do not match</p>}
              </div>
            </CardContent>
            <CardFooter className="flex flex-col gap-4 pt-6">
              <Button type="submit" className="w-full" disabled={isLoading}>
                {isLoading ? <><Loader2 className="mr-2 h-4 w-4 animate-spin" />Creating account...</> : 'Create Account'}
              </Button>
              <p className="text-sm text-muted-foreground text-center">
                Already have an account?{' '}
                <Link to="/login" className="text-primary hover:underline font-medium">Sign in</Link>
              </p>
            </CardFooter>
          </form>
        </Card>
      </div>
    </div>
  );
}
