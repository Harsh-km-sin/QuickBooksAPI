export function StandaloneFooter() {
  return (
    <footer className="shrink-0 w-full py-6 bg-muted/50 border-t border-border">
      <div className="flex flex-col md:flex-row justify-between items-center px-4 md:px-8 max-w-[1440px] mx-auto gap-3">
        <div className="flex items-center gap-3">
          <span className="text-xs font-semibold text-secondary-foreground">QuickBooks Integrated App</span>
          <span className="text-xs text-muted-foreground">&copy; 2026. All rights reserved.</span>
        </div>
        <div className="flex items-center gap-6">
          <a className="text-xs text-muted-foreground hover:text-primary hover:underline transition-colors" href="#">Privacy Policy</a>
          <a className="text-xs text-muted-foreground hover:text-primary hover:underline transition-colors" href="#">Terms of Service</a>
          <a className="text-xs text-muted-foreground hover:text-primary hover:underline transition-colors" href="#">Security</a>
          <a className="text-xs text-muted-foreground hover:text-primary hover:underline transition-colors" href="#">Contact Us</a>
        </div>
      </div>
    </footer>
  );
}
