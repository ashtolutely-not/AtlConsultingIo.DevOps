using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlConsultingIo.DevOps.ODataGenerator;
public record EntitySourceMetadata( SqlTableMetadata SourceTableMeta, FileInfo EntityFileMeta )
{
    public string DbSetName { get; init; } = SourceTableMeta.Name;
}