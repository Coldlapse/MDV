using System.Text;

namespace MDV.Core.Models;

/// <summary>判定したエンコードと表示名</summary>
public record TextEncodingInfo(Encoding Encoding, string DisplayName);
