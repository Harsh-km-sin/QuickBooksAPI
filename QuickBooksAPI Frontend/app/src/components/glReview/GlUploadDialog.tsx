import { useRef, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Upload, Loader2, FileSpreadsheet } from 'lucide-react';
import { useUploadGlFile, useGlRuns } from '@/hooks';

const ACCEPT = '.csv,.xlsx,.xls';

export function GlUploadDialog({ trigger }: { trigger?: React.ReactNode }) {
  const [open, setOpen] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const upload = useUploadGlFile();
  const { data: runs } = useGlRuns();

  // While the upload mutation is settling, the most recent run may still be processing.
  const newest = runs?.[0];
  const inFlight = upload.isPending || (newest != null && (newest.status === 'Pending' || newest.status === 'Processing'));

  const handleSubmit = async () => {
    if (!file) return;
    await upload.mutateAsync(file).catch(() => {});
    setFile(null);
  };

  const reset = () => {
    setFile(null);
    upload.reset();
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        setOpen(o);
        if (!o) reset();
      }}
    >
      <DialogTrigger asChild>
        {trigger ?? (
          <Button>
            <Upload className="h-4 w-4 mr-2" />
            Upload GL
          </Button>
        )}
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Upload General Ledger</DialogTitle>
          <DialogDescription>
            Upload a CSV or Excel export of your general ledger. Analysis runs automatically and may take a
            few minutes.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            className="flex w-full flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-input px-6 py-10 text-center transition-colors hover:bg-muted"
          >
            <FileSpreadsheet className="h-8 w-8 text-muted-foreground" />
            <span className="text-sm font-medium">{file ? file.name : 'Click to choose a file'}</span>
            <span className="text-xs text-muted-foreground">.csv, .xlsx or .xls</span>
          </button>
          <input
            ref={inputRef}
            type="file"
            accept={ACCEPT}
            className="hidden"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />

          {inFlight && newest && (newest.status === 'Pending' || newest.status === 'Processing') && (
            <div className="space-y-1">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Processing “{newest.fileName}”…</span>
                <span>{newest.progressPercentage}%</span>
              </div>
              <Progress value={newest.progressPercentage} />
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Close
          </Button>
          <Button onClick={handleSubmit} disabled={!file || upload.isPending}>
            {upload.isPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Upload className="h-4 w-4 mr-2" />}
            Upload &amp; Analyze
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
