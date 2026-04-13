namespace DomHelpers.SlcDocumenthub
{
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Sections;
    using System;

    /// <summary>
    /// Represents a wrapper class for accessing a CategoryInfoSection section.
    /// The <see cref="CategoryInfoSection"/> class provides simplified access to the data and functionality of the underlying DOM section, allowing for easier manipulation and retrieval of data from DOM.
    /// </summary>
    public partial class CategoryInfoSection : DomSectionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryInfoSection"/> class. Creates an empty <see cref="CategoryInfoSection"/> object with default settings.
        /// </summary>
        public CategoryInfoSection() : base(SlcDocumenthubIds.Sections.CategoryInfo.Id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryInfoSection"/> class using the specified <paramref name="section"/> for initializing the object.
        /// </summary>
        /// <param name="section">The <see cref="Section"/> object that provides data for initializing the <see cref="CategoryInfoSection"/>. If the section is <c>null</c>, the constructor will not perform any initialization.</param>
        public CategoryInfoSection(Section section) : base(section, SlcDocumenthubIds.Sections.CategoryInfo.Id)
        {
        }

        /// <summary>
        /// Gets or sets the Name field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public String Name
        {
            get
            {
                var wrapper = section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Name);
                if (wrapper != null)
                {
                    return (String)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Name);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Name, (String)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Uploadpath field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public String Uploadpath
        {
            get
            {
                var wrapper = section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Uploadpath);
                if (wrapper != null)
                {
                    return (String)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Uploadpath);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Uploadpath, (String)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Description field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public String Description
        {
            get
            {
                var wrapper = section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Description);
                if (wrapper != null)
                {
                    return (String)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Description);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Description, (String)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Storagetype field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public SlcDocumenthubIds.Enums.Storagetype? Storagetype
        {
            get
            {
                var wrapper = section.GetValue<Int32>(SlcDocumenthubIds.Sections.CategoryInfo.Storagetype);
                if (wrapper != null)
                {
                    return (SlcDocumenthubIds.Enums.Storagetype?)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Storagetype);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Storagetype, (Int32)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Extensions field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public String Extensions
        {
            get
            {
                var wrapper = section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Extensions);
                if (wrapper != null)
                {
                    return (String)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Extensions);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Extensions, (String)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Isdefault field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public Boolean? Isdefault
        {
            get
            {
                var wrapper = section.GetValue<Boolean>(SlcDocumenthubIds.Sections.CategoryInfo.Isdefault);
                if (wrapper != null)
                {
                    return (Boolean?)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Isdefault);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Isdefault, (Boolean)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Definition field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public String Definition
        {
            get
            {
                var wrapper = section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Definition);
                if (wrapper != null)
                {
                    return (String)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Definition);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Definition, (String)value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Domsource field of the DOM Instance.
        /// </summary>
        /// <remarks>
        /// When retrieving the value:
        /// <list type="bullet">
        /// <item>If the field has been set, it will return the value.</item>
        /// <item>If the field is not set it will return <see langword="null"/>.</item>
        /// </list>
        /// When setting the value:
        /// <list type="bullet">
        /// <item>- If <see langword="null"/> is assigned, the field will be removed from the section.</item>
        /// <item>- If a valid value is assigned, the field value will be added or updated in the section.</item>
        /// </list>
        /// </remarks>
        public Guid? Domsource
        {
            get
            {
                var wrapper = section.GetValue<Guid>(SlcDocumenthubIds.Sections.CategoryInfo.Domsource);
                if (wrapper != null)
                {
                    return (Guid?)wrapper.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    section.RemoveFieldValueById(SlcDocumenthubIds.Sections.CategoryInfo.Domsource);
                }
                else
                {
                    section.AddOrUpdateValue(SlcDocumenthubIds.Sections.CategoryInfo.Domsource, (Guid)value);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of the current <see cref="CategoryInfoSection"/>.
        /// </summary>
        /// <returns>A new <see cref="CategoryInfoSection"/> object that is a deep copy of this section.</returns>
        public CategoryInfoSection Clone()
        {
            return new CategoryInfoSection((Section)this.ToSection().Clone());
        }

        /// <summary>
        /// Creates a duplicate of the current <see cref="CategoryInfoSection"/> with a new id.
        /// </summary>
        /// <returns>A new <see cref="CategoryInfoSection"/> object that is a copy of this section but with a different id.</returns>
        public CategoryInfoSection Duplicate()
        {
            var section = (Section)this.ToSection().Clone();
            section.ID = new SectionID(Guid.NewGuid());
            return new CategoryInfoSection(section);
        }

        /// <inheritdoc />
        protected override Section InternalToSection()
        {
            if (section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Name) == null)
                throw new InvalidOperationException("'Name' is required. Please fill it in before saving, or mark it as optional with the DOM Editor.");
            if (section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Uploadpath) == null)
                throw new InvalidOperationException("'Uploadpath' is required. Please fill it in before saving, or mark it as optional with the DOM Editor.");
            if (section.GetValue<Int32>(SlcDocumenthubIds.Sections.CategoryInfo.Storagetype) == null)
                throw new InvalidOperationException("'Storagetype' is required. Please fill it in before saving, or mark it as optional with the DOM Editor.");
            if (section.GetValue<String>(SlcDocumenthubIds.Sections.CategoryInfo.Extensions) == null)
                throw new InvalidOperationException("'Extensions' is required. Please fill it in before saving, or mark it as optional with the DOM Editor.");
            return section;
        }
    }
}