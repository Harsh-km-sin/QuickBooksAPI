import { useState, useCallback, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';
import { useQuickBooks } from '@/hooks/useQuickBooks';
import { authApi, setRealmId } from '@/api/client';
import { customerApi } from '@/api/client';
import type { ConnectedCompany } from '@/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Building2, RefreshCw, Loader2, Unplug, Link2 } from 'lucide-react';
import { toast } from 'sonner';

function formatConnectedDate(iso: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

const CONNECT_IMAGE_SRC = '/qbo-buttons/._C2QB_green_btn_med_default.png';

export function ConnectedCompanies() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { user, currentRealmId, setCurrentRealm } = useAuth();
  const { connect, isConnecting } = useQuickBooks();
  const [companies, setCompanies] = useState<ConnectedCompany[]>([]);
  const [connectImageError, setConnectImageError] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [disconnectingId, setDisconnectingId] = useState<string | null>(null);
  const [syncingId, setSyncingId] = useState<string | null>(null);

  const fetchCompanies = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await authApi.getConnectedCompanies();
      if (response.success && response.data) {
        setCompanies(response.data);
      } else {
        setCompanies([]);
      }
    } catch {
      setCompanies([]);
      toast.error('Failed to load connected companies');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchCompanies();
  }, [fetchCompanies]);

  // Handle OAuth callback redirect from backend (?oauth=success | ?oauth=error&message=...)
  useEffect(() => {
    const oauth = searchParams.get('oauth');
    if (!oauth) return;
    const message = searchParams.get('message');
    if (oauth === 'success') {
      toast.success('QuickBooks connected successfully.', {
        description: 'Your company has been linked. You may need to log out and back in to see it in the sidebar.',
      });
      fetchCompanies();
    } else if (oauth === 'error') {
      toast.error('QuickBooks connection failed', {
        description: message ? decodeURIComponent(message) : 'Please try again.',
      });
    }
    // Clear OAuth params from URL
    const next = new URLSearchParams(searchParams);
    next.delete('oauth');
    next.delete('message');
    setSearchParams(next, { replace: true });
  }, [searchParams, setSearchParams, fetchCompanies]);

  const handleDisconnect = async (company: ConnectedCompany) => {
    setDisconnectingId(company.qboRealmId);
    try {
      const response = await authApi.disconnect(company.qboRealmId);
      if (response.success) {
        toast.success('Company disconnected', {
          description: response.message ?? undefined,
        });
        await fetchCompanies();
      } else {
        toast.error(response.message ?? 'Failed to disconnect');
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Failed to disconnect');
    } finally {
      setDisconnectingId(null);
    }
  };

  const handleSync = async (company: ConnectedCompany) => {
    setSyncingId(company.qboRealmId);
    try {
      setRealmId(company.qboRealmId);
      await customerApi.sync();
      toast.success('Sync started', {
        description: `Syncing data for ${company.companyName || company.qboRealmId}. Switch to this company to see updated data.`,
      });
      setCurrentRealm(company.qboRealmId);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Sync failed');
    } finally {
      setSyncingId(null);
    }
  };

  const handleUseCompany = (company: ConnectedCompany) => {
    setCurrentRealm(company.qboRealmId);
  };

  const isDisconnecting = (c: ConnectedCompany) => disconnectingId === c.qboRealmId;
  const isSyncing = (c: ConnectedCompany) => syncingId === c.qboRealmId;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Connected Companies</h1>
        <p className="text-muted-foreground">
          Manage your linked QuickBooks companies. Switch, sync, or disconnect.
        </p>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              QuickBooks Companies
            </CardTitle>
            <CardDescription>
              {user?.realmIds?.length
                ? `You have ${companies.length} connected compan${companies.length !== 1 ? 'ies' : 'y'}.`
                : 'Connect QuickBooks from the sidebar to link a company.'}
            </CardDescription>
          </div>
          {connectImageError ? (
            <Button variant="outline" onClick={connect} disabled={isConnecting} className="shrink-0">
              {isConnecting ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
              Connect to QuickBooks
            </Button>
          ) : (
            <button
              type="button"
              className="shrink-0 cursor-pointer border-0 bg-transparent p-0 disabled:opacity-60 disabled:cursor-not-allowed"
              onClick={connect}
              disabled={isConnecting}
              aria-label="Connect to QuickBooks"
            >
              {isConnecting ? (
                <span className="flex items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Connecting...
                </span>
              ) : (
                <img
                  src={CONNECT_IMAGE_SRC}
                  alt="Connect to QuickBooks"
                  className="h-10 object-contain"
                  onError={() => setConnectImageError(true)}
                />
              )}
            </button>
          )}
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : companies.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              <Building2 className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p className="font-medium">No connected companies</p>
              <p className="text-sm mt-1">Connect QuickBooks from the sidebar to get started.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Company</TableHead>
                  <TableHead>Realm ID</TableHead>
                  <TableHead>Connected</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {companies.map((company) => (
                  <TableRow key={company.qboRealmId}>
                    <TableCell className="font-medium">
                      {company.companyName || `Company ${company.qboRealmId.slice(0, 8)}...`}
                    </TableCell>
                    <TableCell className="font-mono text-muted-foreground text-sm">
                      {company.qboRealmId}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {formatConnectedDate(company.connectedAtUtc)}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleUseCompany(company)}
                          disabled={currentRealmId === company.qboRealmId}
                        >
                          <Link2 className="h-4 w-4 mr-1" />
                          {currentRealmId === company.qboRealmId ? 'In use' : 'Use'}
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleSync(company)}
                          disabled={isSyncing(company)}
                        >
                          {isSyncing(company) ? (
                            <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                          ) : (
                            <RefreshCw className="h-4 w-4 mr-1" />
                          )}
                          Sync
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-destructive hover:text-destructive"
                          onClick={() => handleDisconnect(company)}
                          disabled={isDisconnecting(company)}
                        >
                          {isDisconnecting(company) ? (
                            <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                          ) : (
                            <Unplug className="h-4 w-4 mr-1" />
                          )}
                          Disconnect
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
