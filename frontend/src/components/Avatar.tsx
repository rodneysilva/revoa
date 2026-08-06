import { avatarUrl } from "../lib/avatar";

export function Avatar({
  name,
  src,
  size = 36,
}: {
  name: string;
  src?: string;
  size?: number;
}) {
  return (
    <img
      src={avatarUrl(name, src)}
      alt=""
      width={size}
      height={size}
      loading="lazy"
      onError={(e) => {
        (e.target as HTMLImageElement).style.display = "none";
      }}
      className="rounded-full bg-charcoal border border-smoke object-cover shrink-0"
      style={{ width: size, height: size }}
    />
  );
}
