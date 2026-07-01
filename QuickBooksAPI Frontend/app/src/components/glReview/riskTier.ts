/**
 * Presentation helpers for GL Review risk tiers and anomaly types.
 * Mirrors the labels from the feat/GL-review `flagMeta`.
 */

export type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'success' | 'outline';

export interface TierStyle {
  label: string;
  variant: BadgeVariant;
  /** tailwind text/bg accents for custom chips */
  className?: string;
}

const TIER_STYLES: Record<string, TierStyle> = {
  critical: { label: 'Critical', variant: 'destructive' },
  high: { label: 'High', variant: 'destructive', className: 'bg-orange-500 text-white border-transparent' },
  medium: { label: 'Medium', variant: 'default', className: 'bg-amber-500 text-white border-transparent' },
  low: { label: 'Low', variant: 'secondary' },
  normal: { label: 'Normal', variant: 'outline' },
};

export function tierStyle(tier?: string | null): TierStyle {
  if (!tier) return { label: '—', variant: 'outline' };
  return TIER_STYLES[tier.toLowerCase()] ?? { label: tier, variant: 'outline' };
}

/** A 0–100 score → a tier style, for places that only have the numeric score. */
export function scoreToTierStyle(score?: number | null): TierStyle {
  if (score == null) return { label: '—', variant: 'outline' };
  if (score >= 65) return TIER_STYLES.critical;
  if (score >= 40) return TIER_STYLES.high;
  if (score >= 20) return TIER_STYLES.medium;
  if (score >= 8) return TIER_STYLES.low;
  return TIER_STYLES.normal;
}

export const TIER_ORDER = ['critical', 'high', 'medium', 'low', 'normal'] as const;

export const ANOMALY_LABEL: Record<string, string> = {
  z_score_outlier: 'Amount outlier (Z-score)',
  benford_law: "Benford's Law deviation",
  round_number: 'Round number',
  large_unusual_amount: 'Large unusual amount',
  threshold_breach: 'Threshold breach',
  weekend_posting: 'Weekend posting',
  backdating: 'Possible backdating',
  period_end_cluster: 'Period-end clustering',
  near_duplicate: 'Near-duplicate',
  sequential_round_je: 'Sequential round JEs',
  excessive_adjustments: 'Excessive adjustments',
  split_transaction: 'Possible split transaction',
  reversal: 'Unusual reversal',
  prior_period_adjustment: 'Prior-period adjustment',
  unusual_hour_posting: 'Off-hours posting',
  missing_description: 'Missing description',
  excessive_memo_repetition: 'Repeated memo',
  fraud_pattern_keywords: 'Fraud keywords',
  unusual_account_combo: 'Unusual account pairing',
  related_party_transaction: 'Related-party transaction',
  new_vendor: 'New / one-time vendor',
  manual_journal_entry: 'Manual journal entry',
  transaction_frequency_spike: 'Transaction velocity spike',
  contra_entry: 'Contra entry',
  suspense_account: 'Suspense/clearing account',
  dormant_account_activity: 'Dormant account activity',
  debit_credit_mismatch: 'Journal imbalance',
  intercompany_unusual: 'Large intercompany entry',
  thin_description: 'Thin description',
  high_frequency_same_vendor_amount: 'Repeated identical payments',
  even_cent_amount: 'Round amount anomaly',
  isolation_forest: 'Multivariate outlier (Isolation Forest)',
  dbscan: 'Cluster outlier (DBSCAN)',
  copod: 'COPOD outlier',
  ecod: 'ECOD outlier',
  association_rule: 'Rare account/entity combination',
  account_behavior: 'Unusual account behavior',
  entity_behavior: 'Unusual entity behavior',
  business_rule_breach: 'Business rule breach',
  llm_flagged: 'AI-flagged',
};

export function anomalyLabel(type: string): string {
  return ANOMALY_LABEL[type] ?? type.replace(/_/g, ' ');
}
